using System.Net;
using System.Net.Sockets;
using System.Text;
using vut_ipk1.Common.Enums;
using vut_ipk1.Common.Interfaces;
using vut_ipk1.Tcp.Messages;

namespace vut_ipk1.Tcp;

public class TcpConnection : IConnection
{
    private readonly IPAddress _ip;
    private readonly int _port;
    private readonly int _confirmationTimeout;
    private readonly int _maxRetransmissions;
    
    private FsmState _fsmState = FsmState.Start;
    private string _displayName;
    private TaskCompletionSource<bool> _taskCompletionSource;

    private readonly TcpClient _client = new TcpClient(new IPEndPoint(IPAddress.Any, 0));

    public TcpConnection(IPAddress ip, int port, int confirmationTimeout, int maxRetransmissions)
    {
        this._ip = ip;
        this._port = port;
        this._confirmationTimeout = confirmationTimeout;
        this._maxRetransmissions = maxRetransmissions;
    }

    public async Task MainLoopAsync()
    {
        throw new NotImplementedException();
    }

    public async Task SendMessage(string message)
    {
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
            await _client.ConnectAsync(_ip, _port);
        
        var authMessage = TcpMessageGenerator.GenerateAuthMessage(username, displayName, secret);
        await _client.GetStream().WriteAsync(authMessage);
        await _taskCompletionSource.Task;
    }

    public async Task Join(string channelName)
    {
        throw new NotImplementedException();
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
}