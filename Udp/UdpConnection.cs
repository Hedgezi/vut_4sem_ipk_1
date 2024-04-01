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

    private string _displayName;
    private ushort _messageCounter = 0; // client-sent message ID counter
    private ushort _currentlyWaitingForId = 0; // ID of the message we are currently waiting for (used for REPLY messages)
    private FsmState _fsmState = FsmState.Start;

    private readonly FixedSizeQueue<ushort> _awaitedMessages = new(200); // all CONFIRM messages go here
    private readonly FixedSizeQueue<ushort> _receivedMessages = new(200); // remember received messages for deduplication
    private TaskCompletionSource<bool> _taskCompletionSource; // used for waiting for the server response

    private readonly UdpClient _client = new(new IPEndPoint(IPAddress.Any, 0));

    public UdpConnection(IPAddress ip, int port, int confirmationTimeout, int maxRetransmissions)
    {
        this._ip = ip;
        this._port = port;
        this._confirmationTimeout = confirmationTimeout;
        this._maxRetransmissions = maxRetransmissions;
    }

    /// <inheritdoc />
    public async Task<int> MainLoopAsync()
    {
        while (true)
        {
            var receivedMessage = await _client.ReceiveAsync();
            var message = receivedMessage.Buffer;

            try
            {
                switch ((MessageType)message[0])
                {
                    // if CONFIRM received, add message ID to the list of awaited messages,
                    // so task which waits for this message can gracefully finish
                    case MessageType.CONFIRM:
                        _awaitedMessages.Enqueue(BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan()[1..3]));

                        break;
                    // if REPLY received in Auth or Start state, it must be a REPLY to AUTH message
                    case MessageType.REPLY when _fsmState is FsmState.Auth or FsmState.Start:
                        var (replyAuthMessageId, replyAuthResult, replyAuthRefMessageId, replyAuthMessageContents) =
                            UdpMessageParser.ParseReplyMessage(message);

                        Task.Run(() => AuthReplyRetrieval(replyAuthMessageId, replyAuthResult,
                            replyAuthRefMessageId, replyAuthMessageContents, receivedMessage.RemoteEndPoint));
                        break;
                    // if REPLY received in Open state, it must be a REPLY to JOIN message
                    case MessageType.REPLY when _fsmState is FsmState.Open:
                        var (replyJoinMessageId, replyJoinResult, replyJoinRefMessageId, replyJoinMessageContents) =
                            UdpMessageParser.ParseReplyMessage(message);

                        Task.Run(() => JoinReplyRetrieval(replyJoinMessageId, replyJoinResult,
                            replyJoinRefMessageId, replyJoinMessageContents));
                        break;
                    case MessageType.MSG:
                        var (msgMessageId, msgDisplayName, msgMessageContents) =
                            UdpMessageParser.ParseMsgMessage(message);

                        Task.Run(() => Msg(msgMessageId, msgDisplayName, msgMessageContents));
                        break;
                    case MessageType.ERR:
                        var (errMessageId, errDisplayName, errMessageContents) =
                            UdpMessageParser.ParseMsgMessage(message); // ERR and MSG have the same structure

                        await Err(errMessageId, errDisplayName, errMessageContents);
                        return 1;
                    case MessageType.BYE:
                        _client.Close();

                        return 0;
                    // if unknown message type received, send CONFIRM with the message ID and close the connection
                    default:
                        await SendConfirmMessage(BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan()[1..3]));

                        await ServerError();
                        return 1;
                }
            }
            catch (Exception) // if exception occurred, send CONFIRM with the message ID and close the connection
            {
                if (_fsmState != FsmState.Start && message.Length > 2)
                {
                    await SendConfirmMessage(BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan()[1..3]));
                }
                
                await ServerError();
                return 1;
            }
        }
    }
    
    /// <inheritdoc />
    public async Task SendMessage(string message)
    {
        if (_fsmState != FsmState.Open)
        {
            await Console.Error.WriteLineAsync(ErrorMessage.SendMessageInWrongState);
            return;
        }

        var messageToSend = UdpMessageGenerator.GenerateMsgMessage(_messageCounter, _displayName, message);
        await SendAndAwaitConfirmResponse(messageToSend, _messageCounter++);
    }

    /// <inheritdoc />
    public async Task Auth(string username, string displayName, string secret)
    {
        if (_fsmState is not (FsmState.Start or FsmState.Auth))
        {
            await Console.Error.WriteLineAsync(ErrorMessage.AuthInWrongState);
            return;
        }

        _displayName = displayName;
        _taskCompletionSource = new TaskCompletionSource<bool>();

        var authMessage = UdpMessageGenerator.GenerateAuthMessage(_messageCounter, username, displayName, secret);
        _currentlyWaitingForId = _messageCounter;
        await SendAndAwaitConfirmResponse(authMessage, _messageCounter++,
            _fsmState == FsmState.Auth ? null : new IPEndPoint(_ip, _port)); // send on default endpoint if not established connection on specific port

        await _taskCompletionSource.Task;
    }

    /// <inheritdoc />
    public async Task Join(string channelName)
    {
        if (_fsmState != FsmState.Open)
        {
            await Console.Error.WriteLineAsync(ErrorMessage.JoinInWrongState);
            return;
        }

        _taskCompletionSource = new TaskCompletionSource<bool>();

        var joinMessage = UdpMessageGenerator.GenerateJoinMessage(_messageCounter, channelName, _displayName);
        _currentlyWaitingForId = _messageCounter;
        await SendAndAwaitConfirmResponse(joinMessage, _messageCounter++);

        await _taskCompletionSource.Task;
    }

    /// <inheritdoc />
    public void Rename(string newDisplayName)
    {
        if (_fsmState != FsmState.Open)
        {
            Console.Error.WriteLine(ErrorMessage.RenameInWrongState);
            return;
        }

        _displayName = newDisplayName;
    }

    /// <inheritdoc />
    public async Task EndSession()
    {
        if (_fsmState != FsmState.Start)
        {
            var byeMessage = UdpMessageGenerator.GenerateByeMessage(_messageCounter);
            await SendAndAwaitConfirmResponse(byeMessage, _messageCounter++);
        }

        _client.Close();
        _fsmState = FsmState.End;
    }
    
    /// <summary>
    /// This method is used for processing incoming messages.
    /// </summary>
    /// <param name="messageId">Incoming message ID</param>
    /// <param name="displayName">Incoming message's display name</param>
    /// <param name="messageContents">Incoming message contents</param>
    private async Task Msg(ushort messageId, string displayName, string messageContents)
    {
        await SendConfirmMessage(messageId);

        if (_receivedMessages.Contains(messageId))
            return;

        _receivedMessages.Enqueue(messageId);
        await Console.Out.WriteLineAsync($"{displayName}: {messageContents}");
    }

    /// <summary>
    /// This method is used for processing incoming error messages and ending the session.
    /// </summary>
    /// <param name="messageId">Incoming message ID</param>
    /// <param name="displayName">Incoming message's display name</param>
    /// <param name="messageContents">Incoming message contents</param>
    private async Task Err(ushort messageId, string displayName, string messageContents)
    {
        if (_fsmState != FsmState.Start)
        {
            await SendConfirmMessage(messageId);
        }

        if (_receivedMessages.Contains(messageId))
            return;

        _receivedMessages.Enqueue(messageId);
        await Console.Error.WriteLineAsync($"ERR FROM {displayName}: {messageContents}");

        await EndSession();
    }

    /// <summary>
    /// This method is used for processing incoming REPLY messages after sending AUTH message.
    /// It also handles the connection to the given endpoint.
    /// </summary>
    /// <param name="messageId">Incoming message ID</param>
    /// <param name="result">Incoming message boolean result</param>
    /// <param name="refMessageId">Incoming message referenced ID</param>
    /// <param name="messageContents">Incoming message contents</param>
    /// <param name="endPoint">Incoming message endpoint</param>
    private async Task AuthReplyRetrieval(ushort messageId, bool result, ushort refMessageId, string messageContents,
        IPEndPoint endPoint)
    {
        await SendConfirmMessage(messageId, _fsmState == FsmState.Auth ? null : endPoint);

        if (!IsItNewMessage(messageId))
            return;
        _receivedMessages.Enqueue(messageId);
        
        if (_currentlyWaitingForId != refMessageId)
        {
            await Console.Error.WriteLineAsync(ErrorMessage.ServerError);
            
            await ServerError();
            Environment.Exit(1);
        }
        _currentlyWaitingForId = 0;
        
        // if the connection is not established, connect to the server on the specific port
        if (_fsmState == FsmState.Start)
        {
            await Task.Delay(100);
            _client.Connect(endPoint);
            _fsmState = FsmState.Auth;
        }

        if (!result)
        {
            await Console.Error.WriteLineAsync($"Failure: {messageContents}");
            _taskCompletionSource.SetResult(true);
            return;
        }

        await Console.Error.WriteLineAsync($"Success: {messageContents}");

        _fsmState = FsmState.Open;
        _taskCompletionSource.SetResult(true);
    }

    /// <summary>
    /// This method is used for processing incoming REPLY messages after sending JOIN message.
    /// </summary>
    /// <param name="messageId">Incoming message ID</param>
    /// <param name="result">Incoming message boolean result</param>
    /// <param name="refMessageId">Incoming message referenced ID</param>
    /// <param name="messageContents">Incoming message contents</param>
    private async Task JoinReplyRetrieval(ushort messageId, bool result, ushort refMessageId, string messageContents)
    {
        await SendConfirmMessage(messageId);

        if (!IsItNewMessage(messageId))
            return;
        _receivedMessages.Enqueue(messageId);
        
        if (_currentlyWaitingForId != refMessageId)
        {
            await Console.Error.WriteLineAsync(ErrorMessage.ServerError);
            
            await ServerError();
            Environment.Exit(1);
        }
        _currentlyWaitingForId = 0;

        if (!result)
        {
            await Console.Error.WriteLineAsync($"Failure: {messageContents}");
            _taskCompletionSource.SetResult(true);
            return;
        }
        
        await Console.Error.WriteLineAsync($"Success: {messageContents}");

        _taskCompletionSource.SetResult(true);
    }

    private async Task ServerError()
    {
        if (_fsmState != FsmState.Start)
        {
            var errorMessage =
                UdpMessageGenerator.GenerateErrMessage(_messageCounter, _displayName, ErrorMessage.ServerError);
            await SendAndAwaitConfirmResponse(errorMessage, _messageCounter++);
        }
        
        await Console.Error.WriteLineAsync(ErrorMessage.ServerError);

        await EndSession();
    }

    private async Task SendAndAwaitConfirmResponse(byte[] message, ushort messageId, IPEndPoint? endPoint = null)
    {
        for (var i = 0; i < 1 + _maxRetransmissions; i++)
        {
            await _client.SendAsync(message, message.Length, endPoint);

            await Task.Delay(_confirmationTimeout);

            if (_awaitedMessages.Contains(messageId))
                return;
        }

        // if the message was not confirmed, send error message and close the connection
        await Console.Error.WriteLineAsync(ErrorMessage.NoResponse);
        await ServerError();
        
        Environment.Exit(1);
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