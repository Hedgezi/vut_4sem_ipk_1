namespace vut_ipk1.Common.Interfaces;

public interface IConnection
{
    /// <summary>
    /// Loop for receiving messages from the server and invoking proper methods based on the message type.
    /// If incorrect message was received, the connection will be properly closed.
    /// </summary>
    /// <returns>Task</returns>
    public Task<int> MainLoopAsync();
    
    /// <summary>
    /// Send MSG message to the server with text specified in parameter.
    /// </summary>
    /// <param name="message">Message to be send (must contain only allowed characters, as per IPK24-CHAT specification)</param>
    /// <returns>Task</returns>
    public Task SendMessage(string message);
    
    /// <summary>
    /// Send AUTH message to the server with username, display name and secret specified in parameters and
    /// wait for the REPLY message.
    /// </summary>
    /// <param name="username">Username (must contain only allowed characters, as per IPK24-CHAT specification)</param>
    /// <param name="displayName">Display name (must contain only allowed characters, as per IPK24-CHAT specification)</param>
    /// <param name="secret">Secret (must contain only allowed characters, as per IPK24-CHAT specification)</param>
    /// <returns>Task</returns>
    public Task Auth(string username, string displayName, string secret);
    
    /// <summary>
    /// Send JOIN message to the server with channel ID specified in parameter and wait for the REPLY message.
    /// </summary>
    /// <param name="channelName">Channel ID (must contain only allowed characters, as per IPK24-CHAT specification)</param>
    /// <returns>Task</returns>
    public Task Join(string channelName);

    /// <summary>
    /// Change display name of the user.
    /// </summary>
    /// <param name="newDisplayName">Display name (must contain only allowed characters, as per IPK24-CHAT specification)</param>
    public void Rename(string newDisplayName);

    /// <summary>
    /// Properly close the connection, using BYE message and waiting for its CONFIRM, if needed.
    /// </summary>
    /// <returns>Task</returns>
    public Task EndSession();
}