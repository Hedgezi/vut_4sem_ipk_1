using System.Net;
using vut_ipk1.Common;

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
    private IPEndPoint? _endPoint;
    private string _displayName;

    private System.Net.Sockets.UdpClient _client = new System.Net.Sockets.UdpClient();
    private readonly UdpMessageGenerator _messageGenerator = new UdpMessageGenerator();

    public void Auth(string username, string displayName, string secret)
    {
        var oldEndPoint = new IPEndPoint(ip, port);
        _client.Connect(oldEndPoint);

        var authMessage = _messageGenerator.GenerateAuthMessage(_messageCounter++, username, displayName, secret);
        _client.Send(authMessage, authMessage.Length);
        var receivedResponse = _client.Receive(ref oldEndPoint);

        this._displayName = displayName;
    }
}