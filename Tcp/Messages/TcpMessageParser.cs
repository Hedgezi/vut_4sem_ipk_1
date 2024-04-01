using System.Text;
using System.Text.RegularExpressions;

namespace vut_ipk1.Tcp.Messages;

public static class TcpMessageParser
{
    public static (bool result, string messageContents) ParseReplyMessage(string message)
    {
        var match = Regex.IsMatch(message.ToUpper(), @"^REPLY (OK|NOK) IS [\x20-\x7E]{1,1400}$");
        if (!match)
            throw new ArgumentException("Invalid message format");
        
        var messageParts = message.Trim().Split(' ', 4);
        
        var result = messageParts[1] == "OK";

        return (result, messageParts[3].TrimEnd());
    }

    public static (string displayName, string messageContents) ParseMsgMessage(string message)
    {
        var match = Regex.IsMatch(message.ToUpper(), @"^MSG FROM [\x21-\x7E]{1,20} IS [\x20-\x7E]{1,1400}$");
        if (!match)
            throw new ArgumentException("Invalid message format");
        
        var messageParts = message.Trim().Split(' ', 5);
        
        var displayName = messageParts[2];
        var messageContents = messageParts[4];

        return (displayName, messageContents.TrimEnd());
    }
    
    public static (string displayName, string messageContents) ParseErrMessage(string message)
    {
        var match = Regex.IsMatch(message.ToUpper(), @"^ERR FROM [\x21-\x7E]{1,20} IS [\x20-\x7E]{1,1400}$");
        if (!match)
            throw new ArgumentException("Invalid message format");
        
        var messageParts = message.Trim().Split(' ', 5);
        
        var displayName = messageParts[2];
        var messageContents = messageParts[4];

        return (displayName, messageContents.TrimEnd());
    }
}