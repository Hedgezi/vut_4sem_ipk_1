using System.Text;
using vut_ipk1.Common.Enum;

namespace vut_ipk1.UdpClient;

public class UdpMessageGenerator
{
    private Encoding _encoding = Encoding.ASCII;
    
    public byte[] GenerateConfirmMessage(UInt16 refId)
    {
        var message = new byte[3];
        
        message[0] = (byte)MessageType.CONFIRM;
        message[1] = (byte)(refId >> 8);
        message[2] = (byte)refId;
        
        return message;
    }
    
    public byte[] GenerateReplyMessage(UInt16 id, bool result, UInt16 refId, string contents)
    {
        var contentsBytes = ConvertStringToAsciiBytes(contents);
        
        var message = new byte[1 + 2 + 1 + 2 + contentsBytes.Length];

        message[0] = (byte)MessageType.REPLY;
        message[1] = (byte)(id >> 8);
        message[2] = (byte)id;
        message[3] = (byte)(result ? 1 : 0);
        message[4] = (byte)(refId >> 8);
        message[5] = (byte)refId;
        Array.Copy(contentsBytes, 0, message, 6, contentsBytes.Length);

        return message;
    }

    public byte[] GenerateAuthMessage(UInt16 id, string username, string displayName, string secret)
    {
        var usernameBytes = ConvertStringToAsciiBytes(username);
        var displayNameBytes = ConvertStringToAsciiBytes(displayName);
        var secretBytes = ConvertStringToAsciiBytes(secret);
        
        var message = new byte[1 + 2 + usernameBytes.Length + displayNameBytes.Length + secretBytes.Length];
        
        message[0] = (byte)MessageType.AUTH;
        message[1] = (byte)(id >> 8);
        message[2] = (byte)id;
        
        Array.Copy(usernameBytes, 0, message, 3, usernameBytes.Length);
        Array.Copy(displayNameBytes, 0, message, 3 + usernameBytes.Length, displayNameBytes.Length);
        Array.Copy(secretBytes, 0, message, 3 + usernameBytes.Length + displayNameBytes.Length, secretBytes.Length);

        return message;
    }
    
    public byte[] GenerateJoinMessage(UInt16 id, string channelId, string displayName)
    {
        var channelIdBytes = ConvertStringToAsciiBytes(channelId);
        var displayNameBytes = ConvertStringToAsciiBytes(displayName);
        
        var message = new byte[1 + 2 + channelIdBytes.Length + displayNameBytes.Length];
        
        message[0] = (byte)MessageType.JOIN;
        message[1] = (byte)(id >> 8);
        message[2] = (byte)id;
        
        Array.Copy(channelIdBytes, 0, message, 3, channelIdBytes.Length);
        Array.Copy(displayNameBytes, 0, message, 3 + channelIdBytes.Length, displayNameBytes.Length);

        return message;
    }
    
    public byte[] GenerateMsgMessage(UInt16 id, string displayName, string contents)
    {
        var displayNameBytes = ConvertStringToAsciiBytes(displayName);
        var contentsBytes = ConvertStringToAsciiBytes(contents);
        
        var message = new byte[1 + 2 + displayNameBytes.Length + contentsBytes.Length];
        
        message[0] = (byte)MessageType.MSG;
        message[1] = (byte)(id >> 8);
        message[2] = (byte)id;
        
        Array.Copy(displayNameBytes, 0, message, 3, displayNameBytes.Length);
        Array.Copy(contentsBytes, 0, message, 3 + displayNameBytes.Length, contentsBytes.Length);

        return message;
    }
    
    public byte[] GenerateErrMessage(UInt16 id, string displayName, string contents)
    {
        var displayNameBytes = ConvertStringToAsciiBytes(displayName);
        var contentsBytes = ConvertStringToAsciiBytes(contents);
        
        var message = new byte[1 + 2 + displayNameBytes.Length + contentsBytes.Length];
        
        message[0] = (byte)MessageType.ERR;
        message[1] = (byte)(id >> 8);
        message[2] = (byte)id;
        
        Array.Copy(displayNameBytes, 0, message, 3, displayNameBytes.Length);
        Array.Copy(contentsBytes, 0, message, 3 + displayNameBytes.Length, contentsBytes.Length);

        return message;
    }
    
    public byte[] GenerateByeMessage(UInt16 id)
    {
        var message = new byte[3];
        
        message[0] = (byte)MessageType.BYE;
        message[1] = (byte)(id >> 8);
        message[2] = (byte)id;
        
        return message;
    }

    private byte[] ConvertStringToAsciiBytes(string convertedString)
    {
        var buffer = new byte[_encoding.GetByteCount(convertedString)+1];
        var length = _encoding.GetBytes(convertedString, 0, convertedString.Length, buffer, 0);
        buffer[length - 1] = 0;
        
        return buffer;
    }
}