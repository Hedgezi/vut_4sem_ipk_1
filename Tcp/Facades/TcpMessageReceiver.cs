using System.Net.Sockets;
using System.Text;
using vut_ipk1.Common.Structures;

namespace vut_ipk1.Tcp.Facades;

public class TcpMessageReceiver
{
    private static readonly Encoding ConversionEncoding = Encoding.ASCII;
    private const int BufferSize = 4096;

    private readonly byte[] _buffer = new byte[BufferSize];
    private readonly FixedSizeQueue<string> _queuedMessages = new(50);
    private readonly StringBuilder _messageBuilder = new();

    public async Task<string> ReceiveMessageAsync(TcpClient client)
    {
        if (_queuedMessages.Count > 0)
            return _queuedMessages.Dequeue();
        
        var receivedMessageBytes = await client.GetStream().ReadAsync(_buffer);

        var lastMessageToBuild = receivedMessageBytes == BufferSize && !_buffer[^1].Equals(0x0A) && !_buffer[^2].Equals(0x0D);

        var receivedMessages = ConversionEncoding.GetString(_buffer, 0, receivedMessageBytes).Split("\r\n");
        
        for (var i = 0; i < receivedMessages.Length; i++)
        {
            if (string.IsNullOrEmpty(receivedMessages[i]))
            {
                break;
            }
            
            if (i == receivedMessages.Length - 1 && !lastMessageToBuild)
            {
                _queuedMessages.Enqueue(receivedMessages[i]);
                break;
            } 
            
            if (i == receivedMessages.Length - 1 && lastMessageToBuild)
            {
                _messageBuilder.Append(receivedMessages[i]);
                continue;
            }

            if (_messageBuilder.Length > 0)
            {
                _messageBuilder.Append(receivedMessages[i]);
                _queuedMessages.Enqueue(_messageBuilder.ToString());
                _messageBuilder.Clear();
            }
            else
            {
                _queuedMessages.Enqueue(receivedMessages[i]);
            }
        }
        
        return _queuedMessages.Dequeue();
    }
}