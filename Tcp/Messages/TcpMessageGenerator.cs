namespace vut_ipk1.Tcp.Messages;

public static class TcpMessageGenerator
{
    public static string GenerateAuthMessage(string username, string displayName, string secret)
    {
        return $"AUTH {username} AS {displayName} USING {secret}\r\n";
    }
    
    public static string GenerateJoinMessage(string channelId, string displayName)
    {
        return $"JOIN {channelId} AS {displayName}\r\n";
    }
    
    public static string GenerateMsgMessage(string displayName, string contents)
    {
        return $"MSG FROM {displayName} IS {contents}\r\n";
    }
    
    public static string GenerateErrMessage(string displayName, string contents)
    {
        return $"ERR FROM {displayName} IS {contents}\r\n";
    }
    
    public static string GenerateByeMessage()
    {
        return "BYE\r\n";
    }
}