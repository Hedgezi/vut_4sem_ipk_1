using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Buffers.Binary;
using vut_ipk1.Common;
using vut_ipk1.Common.Enums;
using vut_ipk1.Common.Interfaces;
using vut_ipk1.Common.Structures;
using vut_ipk1.UdpClient.Messages;

namespace vut_ipk1.UdpClient;

public class UdpConnection
    : IConnection
{
    private readonly IPAddress _ip;
    private readonly int _port;
    private readonly int _confirmationTimeout;
    private readonly int _maxRetransmissions;

    private ushort _messageCounter = 0;
    private FsmState _fsmState = FsmState.Start;
    private string _displayName;
    private readonly List<ushort> _awaitedMessages = [];
    private readonly FixedSizeQueue<ushort> _receivedMessages = new(100); // TODO: rework received message logic
    private TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

    private readonly System.Net.Sockets.UdpClient _client = new System.Net.Sockets.UdpClient(new IPEndPoint(IPAddress.Any, 0));

    public UdpConnection(
        IPAddress ip,
        int port,
        int confirmationTimeout,
        int maxRetransmissions
    )
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
            
            if (message[0] == (byte)MessageType.CONFIRM)
            {
                _awaitedMessages.Add(BinaryPrimitives.ReadUInt16LittleEndian(message[1..3]));
            }
            else if (message[0] == (byte)MessageType.REPLY && _fsmState == FsmState.Start)
            { // TODO: state Auth
                var (messageId, result, refMessageId, messageContents) = UdpMessageParser.ParseReplyMessage(message);

                await Task.Run(() => AuthReplyRetrieval(messageId, result, refMessageId, messageContents,
                    receivedMessage.RemoteEndPoint));
            }
            else if (message[0] == (byte)MessageType.MSG)
            {
                var (messageId, displayName, messageContents) = UdpMessageParser.ParseMsgMessage(message);

                await SendConfirmMessage(messageId);

                if (_receivedMessages.Contains(messageId))
                {
                    continue;
                }

                _receivedMessages.Enqueue(messageId);
                await Console.Out.WriteLineAsync($"{displayName}: {messageContents}");
            }
            else if (message[0] == (byte)MessageType.BYE)
            {
                _client.Dispose();

                return;
            }
        }
    }

    public async Task Auth(string username, string displayName, string secret)
    {
        if (this._fsmState != FsmState.Start)
        {
            await Console.Out.WriteLineAsync(ErrorMessage.AuthInWrongState);
            return;
        }

        _displayName = displayName;
        
        _taskCompletionSource = new TaskCompletionSource<bool>();

        var authMessage = UdpMessageGenerator.GenerateAuthMessage(_messageCounter, username, displayName, secret);
        await SendAndAwaitConfirmResponse(authMessage, _messageCounter++,
            new IPEndPoint(_ip, _port));
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

    private async Task AuthReplyRetrieval(ushort messageId, bool result, ushort refMessageId, string messageContents,
        IPEndPoint endPoint)
    {
        await SendConfirmMessage(messageId, endPoint);

        if (!IsItNewMessage(messageId))
        {
            return;
        }

        if (!result)
        {
            await Console.Error.WriteLineAsync($"Failure: {messageContents}");
            return;
        }

        if (refMessageId != this._messageCounter - 1)
        {
            // TODO: error
        }

        await Console.Out.WriteLineAsync($"Success: {messageContents}");

        _receivedMessages.Enqueue(messageId);
        _client.Connect(endPoint);
        _fsmState = FsmState.Open;
        _taskCompletionSource.SetResult(true);
    }

    private async Task SendAndAwaitConfirmResponse(byte[] message, ushort messageId, IPEndPoint? endPoint = null)
    {
        for (var i = 0; i < 1 + _maxRetransmissions; i++)
        {
            if (endPoint == null)
            {
                await _client.SendAsync(message, message.Length);
            }
            else
            {
                await _client.SendAsync(message, message.Length, endPoint);
            }

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
        if (endPoint == null)
        {
            await _client.SendAsync(confirmMessage, confirmMessage.Length);
        }
        else
        {
            await _client.SendAsync(confirmMessage, confirmMessage.Length, endPoint);
        }
    }

    private bool IsItNewMessage(ushort messageId)
    {
        return !_receivedMessages.Contains(messageId);
    }
}