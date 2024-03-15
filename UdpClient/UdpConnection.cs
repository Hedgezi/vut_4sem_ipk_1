using System.Net;
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

    private readonly System.Net.Sockets.UdpClient _client = new System.Net.Sockets.UdpClient();

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
                _awaitedMessages.Add(BitConverter.ToUInt16(message, 1));
            }
            else if (message[0] == (byte)MessageType.REPLY && this._fsmState == FsmState.Start)
            {
                var (messageId, result, refMessageId, messageContents) = UdpMessageParser.ParseReplyMessage(message);
                
                await Task.Run(() => AuthReplyRetrieval(messageId, result, refMessageId, messageContents, receivedMessage.RemoteEndPoint));
            }
            else if (message[0] == (byte)MessageType.MSG)
            {
                var (messageId, displayName, messageContents) = UdpMessageParser.ParseMsgMessage(message);

                await SendConfirmMessage(messageId);
                
                var confirmMessage = UdpMessageGenerator.GenerateConfirmMessage(messageId);
                await _client.SendAsync(confirmMessage, confirmMessage.Length, receivedMessage.RemoteEndPoint);
                
                if (_receivedMessages.Contains(messageId))
                {
                    continue;
                }

                _receivedMessages.Enqueue(messageId);
                await Console.Out.WriteLineAsync($"{displayName}: {messageContents}");
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

        this._displayName = displayName;

        var authMessage = UdpMessageGenerator.GenerateAuthMessage(_messageCounter, username, displayName, secret);
        await SendAndAwaitConfirmResponse(authMessage, _messageCounter++, new IPEndPoint(_ip, _port)); // TODO: check if it returned
    }

    public void Rename(string newDisplayName)
    {
        if (this._fsmState != FsmState.Open)
        {
            Console.WriteLine(ErrorMessage.RenameInWrongState);
            return;
        }

        this._displayName = newDisplayName;
    }
    
    private async Task AuthReplyRetrieval(ushort messageId, bool result, ushort refMessageId, string messageContents, IPEndPoint endPoint)
    {
        if (!result)
        {
            await Console.Error.WriteLineAsync($"Failure: {messageContents}");
            return;
        }

        if (refMessageId != this._messageCounter - 1)
        {
            // TODO: error
        }

        await Console.Out.WriteLineAsync($"");
        
        this._receivedMessages.Enqueue(messageId);
        _client.Connect(endPoint);
        this._fsmState = FsmState.Open;
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