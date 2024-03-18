using System.Text;
using System.Text.RegularExpressions;

namespace vut_ipk1.Tcp.Messages;

public static class TcpMessageParser
{
    private static readonly Encoding TextEncoding = Encoding.ASCII;
    
    public static (bool result, string messageContents) ParseReplyMessage(byte[] message)
    {
        var messageString = TextEncoding.GetString(message);
        
        var match = Regex.IsMatch(messageString, @"^REPLY (OK|NOK) IS [\x20-\x7E]{1,1400}\r\n$");
        if (!match)
            throw new ArgumentException("Invalid message format");
        
        var messageParts = messageString.Trim().Split(' ');
        
        var result = messageParts[1] == "OK";

        return (result, messageParts[3]);
    }

    public static (string displayName, string messageContents) ParseMsgMessage(byte[] message)
    {
        var messageString = TextEncoding.GetString(message);
        
        var match = Regex.IsMatch(messageString, @"^MSG FROM [\x21-\x7E]{1,20} IS [\x20-\x7E]{1,1400}\r\n$");
        if (!match)
            throw new ArgumentException("Invalid message format");
        
        var messageParts = messageString.Trim().Split(' ');
        
        var displayName = messageParts[2];
        var messageContents = messageParts[4..];

        return (displayName, messageContents.ToString());
    }
    
    public static (string displayName, string messageContents) ParseErrMessage(byte[] message)
    {
        var messageString = TextEncoding.GetString(message);

        var match = Regex.IsMatch(messageString, @"^ERR FROM [\x21-\x7E]{1,20} IS [\x20-\x7E]{1,1400}\r\n$");
        if (!match)
            throw new ArgumentException("Invalid message format");
        
        var messageParts = messageString.Trim().Split(' ');
        
        var displayName = messageParts[2];
        var messageContents = messageParts[4..];

        return (displayName, messageContents.ToString());
    }
}