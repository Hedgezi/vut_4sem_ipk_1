using System.Net;
using System.Net.Sockets;
using vut_ipk1.Common.Enums;
using vut_ipk1.Common.Interfaces;
using vut_ipk1.Common.Structures;

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

    public Task MainLoopAsync()
    {
        throw new NotImplementedException();
    }

    public Task SendMessage(string message)
    {
        throw new NotImplementedException();
    }

    public Task Auth(string username, string displayName, string secret)
    {
        throw new NotImplementedException();
    }

    public Task Join(string channelName)
    {
        throw new NotImplementedException();
    }

    public void Rename(string newDisplayName)
    {
        throw new NotImplementedException();
    }

    public Task EndSession()
    {
        throw new NotImplementedException();
    }
}