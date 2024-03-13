namespace vut_ipk1.Common;

public interface IConnection
{
    public Task MainLoopAsync();
    
    public void Auth(string username, string displayName, string secret);

    public void Rename(string newDisplayName);
}