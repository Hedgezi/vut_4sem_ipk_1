namespace vut_ipk1.Common;

public interface IConnection
{
    public void Auth(string username, string displayName, string secret);
}