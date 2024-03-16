using System.Net;
using System.Buffers.Binary;
using System.Net.Sockets;
using vut_ipk1.Common.Enums;
using vut_ipk1.Common.Interfaces;
using vut_ipk1.Common.Structures;
using vut_ipk1.Udp.Messages;

namespace vut_ipk1.Udp;

public class UdpConnection : IConnection
{
    private readonly IPAddress _ip;
    private readonly int _port;
    private readonly int _confirmationTimeout;
    private readonly int _maxRetransmissions;

    private ushort _messageCounter = 0;
    private FsmState _fsmState = FsmState.Start;
    private string _displayName;
    private readonly List<ushort> _awaitedMessages = [];
    private readonly FixedSizeQueue<ushort> _receivedMessages = new(100);
    private TaskCompletionSource<bool> _taskCompletionSource;

    private readonly UdpClient _client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));

    public UdpConnection(IPAddress ip, int port, int confirmationTimeout, int maxRetransmissions)
    {
        this._ip = ip;
        this._port = port;
        this._confirmationTimeout = confirmationTimeout;
        this._maxRetransmissions = maxRetransmissions;
    }

    public async Task MainLoopAsync()
    {
        while (true)
        {
            var receivedMessage = await _client.ReceiveAsync();
            var message = receivedMessage.Buffer;
            var messageType = (MessageType)message[0];

            switch (messageType)
            {
                case MessageType.CONFIRM:
                    _awaitedMessages.Add(BinaryPrimitives.ReadUInt16LittleEndian(message.AsSpan()[1..3]));

                    break;
                case MessageType.REPLY when _fsmState is FsmState.Auth:
                    var (replyAuthMessageId, replyAuthResult, replyAuthRefMessageId, replyAuthMessageContents) =
                        UdpMessageParser.ParseReplyMessage(message);

                    await Task.Run(() => AuthReplyRetrieval(replyAuthMessageId, replyAuthResult,
                        replyAuthRefMessageId, replyAuthMessageContents, receivedMessage.RemoteEndPoint));
                    break;
                case MessageType.REPLY when _fsmState is FsmState.Open:
                    var (replyJoinMessageId, replyJoinResult, replyJoinRefMessageId, replyJoinMessageContents) =
                        UdpMessageParser.ParseReplyMessage(message);

                    await Task.Run(() => JoinReplyRetrieval(replyJoinMessageId, replyJoinResult,
                        replyJoinRefMessageId, replyJoinMessageContents));
                    break;
                case MessageType.MSG:
                    var (msgMessageId, msgDisplayName, msgMessageContents) = UdpMessageParser.ParseMsgMessage(message);

                    await Task.Run(() => Msg(msgMessageId, msgDisplayName, msgMessageContents));
                    break;
                case MessageType.BYE:
                    _client.Dispose();
                    
                    return;
            }
        }
    }

    public async Task Auth(string username, string displayName, string secret)
    {
        if (_fsmState is not (FsmState.Start or FsmState.Auth))
        {
            await Console.Out.WriteLineAsync(ErrorMessage.AuthInWrongState);
            return;
        }

        _displayName = displayName;
        _taskCompletionSource = new TaskCompletionSource<bool>();
        _fsmState = FsmState.Auth;

        var authMessage = UdpMessageGenerator.GenerateAuthMessage(_messageCounter, username, displayName, secret);
        await SendAndAwaitConfirmResponse(authMessage, _messageCounter++,
            // _client.Client.RemoteEndPoint == null ? null : new IPEndPoint(_ip, _port));
            new IPEndPoint(_ip, _port));
        await _taskCompletionSource.Task;
    }

    public async Task Join(string channelName)
    {
        if (_fsmState != FsmState.Open)
        {
            await Console.Out.WriteLineAsync(ErrorMessage.JoinInWrongState);
            return;
        }

        _taskCompletionSource = new TaskCompletionSource<bool>();

        var joinMessage = UdpMessageGenerator.GenerateJoinMessage(_messageCounter, _displayName, channelName);
        await SendAndAwaitConfirmResponse(joinMessage, _messageCounter++);
        await _taskCompletionSource.Task;
    }

    public async Task SendMessage(string message)
    {
        if (_fsmState != FsmState.Open)
        {
            await Console.Out.WriteLineAsync(ErrorMessage.SendMessageInWrongState);
            return;
        }

        var messageToSend = UdpMessageGenerator.GenerateMsgMessage(_messageCounter, _displayName, message);
        await SendAndAwaitConfirmResponse(messageToSend, _messageCounter++);
    }

    public void Rename(string newDisplayName)
    {
        if (_fsmState != FsmState.Open)
        {
            Console.WriteLine(ErrorMessage.RenameInWrongState);
            return;
        }

        _displayName = newDisplayName;
    }

    public async Task EndSession()
    {
        _client.Dispose();
    
        if (_fsmState is FsmState.Start)
        {
            return;
        }
        
        var byeMessage = UdpMessageGenerator.GenerateByeMessage(_messageCounter);
        await SendAndAwaitConfirmResponse(byeMessage, _messageCounter++);
        
        _fsmState = FsmState.End;
    }

    private async Task AuthReplyRetrieval(ushort messageId, bool result, ushort refMessageId, string messageContents,
        IPEndPoint endPoint)
    {
        await SendConfirmMessage(messageId, endPoint);

        if (!IsItNewMessage(messageId))
            return;

        if (!result)
        {
            await Console.Error.WriteLineAsync($"Failure: {messageContents}");
            return;
        }

        if (refMessageId != _messageCounter - 1)
        {
            // TODO: error
        }

        await Console.Out.WriteLineAsync($"Success: {messageContents}");

        _receivedMessages.Enqueue(messageId);
        _client.Connect(endPoint);
        _fsmState = FsmState.Open;
        _taskCompletionSource.SetResult(true);
    }

    private async Task JoinReplyRetrieval(ushort messageId, bool result, ushort refMessageId, string messageContents)
    {
        await SendConfirmMessage(messageId);

        if (!IsItNewMessage(messageId))
            return;

        if (!result)
        {
            await Console.Error.WriteLineAsync($"Failure: {messageContents}");
            return;
        }

        if (refMessageId != _messageCounter - 1)
        {
            // TODO: error
        }

        await Console.Out.WriteLineAsync($"Success: {messageContents}");

        _receivedMessages.Enqueue(messageId);
        _taskCompletionSource.SetResult(true);
    }

    private async Task Msg(ushort messageId, string displayName, string messageContents)
    {
        await SendConfirmMessage(messageId);

        if (_receivedMessages.Contains(messageId))
            return;

        _receivedMessages.Enqueue(messageId);
        await Console.Out.WriteLineAsync($"{displayName}: {messageContents}");
    }

    private async Task SendAndAwaitConfirmResponse(byte[] message, ushort messageId, IPEndPoint? endPoint = null)
    {
        for (var i = 0; i < 1 + _maxRetransmissions; i++)
        {
            await _client.SendAsync(message, message.Length, endPoint);

            await Task.Delay(_confirmationTimeout);

            if (_awaitedMessages.Remove(messageId))
            {
                return;
            }
        }
    }

    private async Task SendConfirmMessage(ushort messageId, IPEndPoint? endPoint = null)
    {
        var confirmMessage = UdpMessageGenerator.GenerateConfirmMessage(messageId);

        await _client.SendAsync(confirmMessage, confirmMessage.Length, endPoint);
    }

    private bool IsItNewMessage(ushort messageId)
    {
        return !_receivedMessages.Contains(messageId);
    }
}