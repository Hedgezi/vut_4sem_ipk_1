using System.Text.RegularExpressions;

namespace vut_ipk1.Tcp.Messages;

public static class TcpMessageParser
{
    public static (bool result, string messageContents) ParseReplyMessage(string message)
    {
        var match = Regex.IsMatch(message, @"^REPLY (OK|NOK) IS [\x20-\x7E]{1,1400}\r\n$");
        if (!match)
            throw new ArgumentException("Invalid message format");
        
        var messageParts = message.Split(' ');
        
        var result = messageParts[1] == "OK";

        return (result, messageParts[3]);
    }

    public static (string displayName, string messageContents) ParseMsgMessage(string message)
    {
        var match = Regex.IsMatch(message, @"^MSG FROM [\x21-\x7E]{1,20} IS [\x20-\x7E]{1,1400}\r\n$");
        if (!match)
            throw new ArgumentException("Invalid message format");
        
        var messageParts = message.Split(' ');
        
        var displayName = messageParts[2];
        var messageContents = messageParts[4];

        return (displayName, messageContents);
    }
    
    public static (string displayName, string messageContents) ParseErrMessage(string message)
    {
        var match = Regex.IsMatch(message, @"^ERR FROM [\x21-\x7E]{1,20} IS [\x20-\x7E]{1,1400}\r\n$");
        if (!match)
            throw new ArgumentException("Invalid message format");
        
        var messageParts = message.Split(' ');
        
        var displayName = messageParts[2];
        var messageContents = messageParts[4];

        return (displayName, messageContents);
    }
}