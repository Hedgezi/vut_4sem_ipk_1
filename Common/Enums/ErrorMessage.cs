namespace vut_ipk1.Common.Enums;

public static class ErrorMessage
{
    private const string Error = "ERR: ";
    
    public const string SendMessageInWrongState = Error + "You can't send a message, if you're not authenticated.";
    
    public const string AuthInWrongState = Error + "You already authenticated.";
    
    public const string JoinInWrongState = Error + "You can't join a channel, if you're not authenticated.";
    
    public const string RenameInWrongState = Error + "You can't rename, if you're not authenticated.";
    
    public const string ServerError = Error + "Server error.";
    
    public const string NoResponse = Error + "No response from server.";
    
    public const string InvalidCommandUsage = Error + "Invalid command usage.";

    public const string InvalidUsername = Error + "Invalid username.";
    
    public const string InvalidSecret = Error + "Invalid secret.";
    
    public const string InvalidDisplayName = Error + "Invalid display name.";
    
    public const string InvalidChannelName = Error + "Invalid channel name.";
    
    public const string InvalidMessage = Error + "Invalid message.";
}