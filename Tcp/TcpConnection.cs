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
    private TaskCompletionSource<bool> _taskCompletionSource;
    private readonly TaskCompletionSource<bool> _isConnected = new TaskCompletionSource<bool>();

    private readonly TcpClient _client = new TcpClient(new IPEndPoint(IPAddress.Any, 0));
    private readonly TcpMessageReceiver _tcpMessageReceiver = new();

    public TcpConnection(IPAddress ip, int port)
    {
        this._ip = ip;
        this._port = port;
    }

    public async Task MainLoopAsync()
    {
        await _isConnected.Task;
            
        while (true)
        {
            var receivedMessage = await _tcpMessageReceiver.ReceiveMessageAsync(_client);

            switch ((MessageType)Enum.Parse(typeof(MessageType), receivedMessage.Split(' ')[0]))
            {
                case MessageType.REPLY when _fsmState is FsmState.Auth or FsmState.Start:
                    var (replyAuthResult, replyAuthMessageContents) = TcpMessageParser.ParseReplyMessage(receivedMessage);

                    Task.Run(() => AuthReplyRetrieval(replyAuthResult, replyAuthMessageContents));
                    break;
                case MessageType.REPLY when _fsmState is FsmState.Open:
                    var (replyJoinResult, replyJoinMessageContents) = TcpMessageParser.ParseReplyMessage(receivedMessage);

                    Task.Run(() => JoinReplyRetrieval(replyJoinResult, replyJoinMessageContents));
                    break;
                case MessageType.MSG:
                    var (msgDisplayName, msgMessageContents) = TcpMessageParser.ParseMsgMessage(receivedMessage);

                    await Console.Out.WriteLineAsync($"{msgDisplayName}: {msgMessageContents}");
                    break;
                case MessageType.ERR:
                    var (errDisplayName, errMessageContents) = TcpMessageParser.ParseMsgMessage(receivedMessage);
                    
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
    }

    public async Task SendMessage(string message)
    {
        if (_fsmState != FsmState.Open)
        {
            await Console.Out.WriteLineAsync(ErrorMessage.SendMessageInWrongState);
            return;
        }
        
        var msgMessage = TcpMessageGenerator.GenerateMsgMessage(_displayName, message);
        await _client.GetStream().WriteAsync(msgMessage);
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

        if (!_client.Connected)
        {
            await _client.ConnectAsync(_ip, _port);
            _isConnected.SetResult(true);
        }

        var authMessage = TcpMessageGenerator.GenerateAuthMessage(username, displayName, secret);
        await _client.GetStream().WriteAsync(authMessage);
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
        
        var joinMessage = TcpMessageGenerator.GenerateJoinMessage(channelName, _displayName);
        await _client.GetStream().WriteAsync(joinMessage);
        await _taskCompletionSource.Task;
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
        if (_fsmState is FsmState.Start)
        {
            _client.Close();
            _client.Dispose();
            return;
        }
        
        var byeMessage = TcpMessageGenerator.GenerateByeMessage();
        await _client.GetStream().WriteAsync(byeMessage);
        
        _client.Close();
        _client.Dispose();
        _fsmState = FsmState.End;
    }
    
    private async Task AuthReplyRetrieval(bool result, string messageContents)
    {
        _fsmState = FsmState.Auth;
        
        if (!result)
        {
            await Console.Error.WriteLineAsync($"Failure: {messageContents}");
            return;
        }

        await Console.Out.WriteLineAsync($"Success: {messageContents}");
        
        _fsmState = FsmState.Open;
        _taskCompletionSource.SetResult(true);
    }
    
    private async Task JoinReplyRetrieval(bool result, string messageContents)
    {
        if (!result)
        {
            await Console.Error.WriteLineAsync($"Failure: {messageContents}");
            return;
        }

        await Console.Out.WriteLineAsync($"Success: {messageContents}");

        _taskCompletionSource.SetResult(true);
    }
    
    private async Task ServerError()
    {
        var errorMessage = TcpMessageGenerator.GenerateErrMessage(_displayName, ErrorMessage.ServerError);
        await _client.GetStream().WriteAsync(errorMessage);

        await EndSession();
    }
}