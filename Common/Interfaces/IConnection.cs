namespace vut_ipk1.Common.Interfaces;

public interface IConnection
{
    public Task MainLoopAsync();
    
    public Task SendMessage(string message);
    
    public Task Join(string channelName);
    
    public Task Auth(string username, string displayName, string secret);

    public void Rename(string newDisplayName);
}