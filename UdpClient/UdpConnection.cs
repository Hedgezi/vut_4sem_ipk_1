using System.Net;
using vut_ipk1.Common;
using vut_ipk1.Common.Enum;
using vut_ipk1.UdpClient.Records;

namespace vut_ipk1.UdpClient;

public class UdpConnection(
    IPAddress ip,
    int port,
    int confirmationTimeout,
    int maxRetransmissions
)
    : IConnection
{
    private UInt16 _messageCounter = 0;
    private FsmState _fsmState = FsmState.Start;
    private List<AwaitedMessage> _awaitedMessages = [];
    
    private IPEndPoint? _endPoint;
    private string _displayName;

    private readonly System.Net.Sockets.UdpClient _client = new System.Net.Sockets.UdpClient();
    private readonly UdpMessageGenerator _messageGenerator = new UdpMessageGenerator();
    
    public async Task MainLoopAsync()
    {
        while (true)
        {
            if (this._fsmState != FsmState.Open)
            {
                await Task.Delay(1000);
                continue;
            }
            
            var receivedMessage = await _client.ReceiveAsync();
        }
    }

    public void Auth(string username, string displayName, string secret)
    {
        if (this._fsmState != FsmState.Start)
        {
            Console.WriteLine(ErrorMessage.AuthInWrongState);
            return;
        }
        
        var oldEndPoint = new IPEndPoint(ip, port);
        _client.Connect(oldEndPoint);

        var authMessage = _messageGenerator.GenerateAuthMessage(_messageCounter++, username, displayName, secret);
        
        _client.Send(authMessage, authMessage.Length);
        var newEndPoint = new IPEndPoint(ip, 0);

        try
        {
            var receivedResponse = _client.Receive(ref newEndPoint);
            
            Console.WriteLine(receivedResponse);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        this._displayName = displayName;
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
}