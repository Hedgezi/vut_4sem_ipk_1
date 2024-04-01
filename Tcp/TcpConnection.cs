using System.Net;
using System.Net.Sockets;
using System.Text;
using vut_ipk1.Common.Enums;
using vut_ipk1.Common.Interfaces;
using vut_ipk1.Tcp.Facades;
using vut_ipk1.Tcp.Messages;

namespace vut_ipk1.Tcp;

public class TcpConnection : IConnection
{
    private readonly IPAddress _ip;
    private readonly int _port;
    
    private FsmState _fsmState = FsmState.Start;
    private string _displayName;
    private TaskCompletionSource<bool> _taskCompletionSource; // Used for waiting for server response
    private readonly TaskCompletionSource<bool> _isConnected = new(); // Used for waiting for connection to be established, to run main loop

    private readonly TcpClient _client = new(new IPEndPoint(IPAddress.Any, 0));
    private readonly TcpMessageReceiver _tcpMessageReceiver = new();

    public TcpConnection(IPAddress ip, int port)
    {
        this._ip = ip;
        this._port = port;
    }

    /// <inheritdoc />
    public async Task MainLoopAsync()
    {
        await _isConnected.Task; // Wait for connection to be established
            
        while (true)
        {
            var receivedMessage = await _tcpMessageReceiver.ReceiveMessageAsync(_client);

            try
            {
                switch ((MessageType)Enum.Parse(typeof(MessageType), receivedMessage.Split(' ')[0]))
                {
                    case MessageType.REPLY when _fsmState is FsmState.Auth or FsmState.Start:
                        var (replyAuthResult, replyAuthMessageContents) =
                            TcpMessageParser.ParseReplyMessage(receivedMessage);

                        Task.Run(() => AuthReplyRetrieval(replyAuthResult, replyAuthMessageContents));
                        break;
                    case MessageType.REPLY when _fsmState is FsmState.Open:
                        var (replyJoinResult, replyJoinMessageContents) =
                            TcpMessageParser.ParseReplyMessage(receivedMessage);

                        Task.Run(() => JoinReplyRetrieval(replyJoinResult, replyJoinMessageContents));
                        break;
                    case MessageType.MSG:
                        var (msgDisplayName, msgMessageContents) = TcpMessageParser.ParseMsgMessage(receivedMessage);

                        await Console.Out.WriteLineAsync($"{msgDisplayName}: {msgMessageContents}");
                        break;
                    case MessageType.ERR:
                        var (errDisplayName, errMessageContents) = TcpMessageParser.ParseErrMessage(receivedMessage);

                        await Console.Error.WriteLineAsync($"ERR FROM {errDisplayName}: {errMessageContents}");
                        await EndSession();
                        return;
                    case MessageType.BYE:
                        _client.Close();

                        return;
                    default:
                        await ServerError();

                        return;
                }
            }
            catch (Exception)
            {
                await ServerError();

                return;
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
        
        var msgMessage = TcpMessageGenerator.GenerateMsgMessage(_displayName, message);
        await _client.GetStream().WriteAsync(msgMessage);
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

        if (!_client.Connected)
        {
            await _client.ConnectAsync(_ip, _port);
            _fsmState = FsmState.Auth;
            _isConnected.SetResult(true);
        }

        var authMessage = TcpMessageGenerator.GenerateAuthMessage(username, displayName, secret);
        await _client.GetStream().WriteAsync(authMessage);
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
        
        var joinMessage = TcpMessageGenerator.GenerateJoinMessage(channelName, _displayName);
        await _client.GetStream().WriteAsync(joinMessage);
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
            var byeMessage = TcpMessageGenerator.GenerateByeMessage();
            await _client.GetStream().WriteAsync(byeMessage);
        }
        
        _client.GetStream().Close();
        _client.Close();
        _fsmState = FsmState.End;
    }
    
    /// <summary>
    /// This method is used for processing incoming REPLY messages after sending AUTH message.
    /// It also using TaskCompletionSource releases waiting in the MainLoopAsync method,
    /// so it starts to receive and process incoming messages.
    /// </summary>
    /// <param name="result">Incoming message boolean result</param>
    /// <param name="messageContents">Incoming message contents</param>
    private async Task AuthReplyRetrieval(bool result, string messageContents)
    {
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
    /// <param name="result">Incoming message boolean result</param>
    /// <param name="messageContents">Incoming message contents</param>
    private async Task JoinReplyRetrieval(bool result, string messageContents)
    {
        if (!result)
        {
            await Console.Error.WriteLineAsync($"Failure: {messageContents}");
            _taskCompletionSource.SetResult(true);
            return;
        }

        await Console.Error.WriteLineAsync($"Success: {messageContents}");

        _taskCompletionSource.SetResult(true);
    }
    
    /// <summary>
    /// This method is called when message from server is corrupted or in wrong format.
    /// It sends ERR message to the server and properly closes the connection.
    /// </summary>
    private async Task ServerError()
    {
        if (_fsmState != FsmState.Start)
        {
            var errorMessage = TcpMessageGenerator.GenerateErrMessage(_displayName, ErrorMessage.ServerError);
            await _client.GetStream().WriteAsync(errorMessage);
        }

        await Console.Error.WriteLineAsync(ErrorMessage.ServerError);

        await EndSession();
    }
}