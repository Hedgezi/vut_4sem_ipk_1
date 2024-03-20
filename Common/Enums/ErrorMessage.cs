namespace vut_ipk1.Common.Enums;

public static class ErrorMessage
{
    public const string AuthInWrongState = "You already authenticated.";
    
    public const string RenameInWrongState = "You can't rename, if you're not authenticated.";
    
    public const string SendMessageInWrongState = "You can't send a message, if you're not authenticated.";
    
    public const string JoinInWrongState = "You can't join a channel, if you're not authenticated.";
    
    public const string ServerError = "Server error.";
    
    public const string NoResponse = "No response from server.";
}