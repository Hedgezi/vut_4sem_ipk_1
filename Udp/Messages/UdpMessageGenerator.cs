using System.Buffers.Binary;
using System.Text;
using vut_ipk1.Common.Enums;

namespace vut_ipk1.Udp.Messages;

public static class UdpMessageGenerator
{
    private static readonly Encoding TextEncoding = Encoding.ASCII;
    
    public static byte[] GenerateConfirmMessage(ushort id)
    {
        var message = new byte[3];
        
        message[0] = (byte)MessageType.CONFIRM;
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(1, 2), id);
        
        return message;
    }
    
    public static byte[] GenerateAuthMessage(ushort id, string username, string displayName, string secret)
    {
        var usernameBytes = ConvertStringToAsciiBytes(username);
        var displayNameBytes = ConvertStringToAsciiBytes(displayName);
        var secretBytes = ConvertStringToAsciiBytes(secret);
        
        var message = new byte[1 + 2 + usernameBytes.Length + displayNameBytes.Length + secretBytes.Length];
        
        message[0] = (byte)MessageType.AUTH;
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(1, 2), id);
        
        Array.Copy(usernameBytes, 0, message, 3, usernameBytes.Length);
        Array.Copy(displayNameBytes, 0, message, 3 + usernameBytes.Length, displayNameBytes.Length);
        Array.Copy(secretBytes, 0, message, 3 + usernameBytes.Length + displayNameBytes.Length, secretBytes.Length);

        return message;
    }
    
    public static byte[] GenerateJoinMessage(ushort id, string channelId, string displayName)
    {
        var channelIdBytes = ConvertStringToAsciiBytes(channelId);
        var displayNameBytes = ConvertStringToAsciiBytes(displayName);
        
        var message = new byte[1 + 2 + channelIdBytes.Length + displayNameBytes.Length];
        
        message[0] = (byte)MessageType.JOIN;
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(1, 2), id);
        
        Array.Copy(channelIdBytes, 0, message, 3, channelIdBytes.Length);
        Array.Copy(displayNameBytes, 0, message, 3 + channelIdBytes.Length, displayNameBytes.Length);

        return message;
    }
    
    public static byte[] GenerateMsgMessage(ushort id, string displayName, string contents)
    {
        var displayNameBytes = ConvertStringToAsciiBytes(displayName);
        var contentsBytes = ConvertStringToAsciiBytes(contents);
        
        var message = new byte[1 + 2 + displayNameBytes.Length + contentsBytes.Length];
        
        message[0] = (byte)MessageType.MSG;
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(1, 2), id);
        
        Array.Copy(displayNameBytes, 0, message, 3, displayNameBytes.Length);
        Array.Copy(contentsBytes, 0, message, 3 + displayNameBytes.Length, contentsBytes.Length);

        return message;
    }
    
    public static byte[] GenerateErrMessage(ushort id, string displayName, string contents)
    {
        var displayNameBytes = ConvertStringToAsciiBytes(displayName);
        var contentsBytes = ConvertStringToAsciiBytes(contents);
        
        var message = new byte[1 + 2 + displayNameBytes.Length + contentsBytes.Length];
        
        message[0] = (byte)MessageType.ERR;
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(1, 2), id);
        
        Array.Copy(displayNameBytes, 0, message, 3, displayNameBytes.Length);
        Array.Copy(contentsBytes, 0, message, 3 + displayNameBytes.Length, contentsBytes.Length);

        return message;
    }
    
    public static byte[] GenerateByeMessage(ushort id)
    {
        var message = new byte[3];
        
        message[0] = (byte)MessageType.BYE;
        BinaryPrimitives.WriteUInt16BigEndian(message.AsSpan(1, 2), id);
        
        return message;
    }

    /// <summary>
    /// This method converts the string to ASCII bytes and appends null byte at the end.
    /// </summary>
    /// <param name="convertedString">String to convert</param>
    /// <returns>Converted byte array</returns>
    private static byte[] ConvertStringToAsciiBytes(string convertedString)
    {
        var buffer = new byte[TextEncoding.GetByteCount(convertedString)+1];
        var length = TextEncoding.GetBytes(convertedString, 0, convertedString.Length, buffer, 0);
        buffer[length] = 0;
        
        return buffer;
    }
}