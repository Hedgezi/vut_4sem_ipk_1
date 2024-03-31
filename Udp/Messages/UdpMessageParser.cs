using System.Buffers.Binary;
using System.Text;

namespace vut_ipk1.Udp.Messages;

public static class UdpMessageParser
{
    private static readonly Encoding TextEncoding = Encoding.ASCII;

    public static (ushort messageId, bool result, ushort refMessageId, string messageContents) ParseReplyMessage(
        byte[] message)
    {
        if (message[3] is not 0x00 and not 0x01)
            throw new ArgumentException("Invalid result value in REPLY message.");
        
        var messageId = BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan()[1..3]);
        var result = message[3] == 0x01;
        var refMessageId = BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan()[4..6]);
        var messageContents = ConvertAsciiBytesToString(message, 6);

        return (messageId, result, refMessageId, messageContents);
    }

    public static (ushort messageId, string displayName, string messageContents) ParseMsgMessage(byte[] message)
    {
        var messageId = BinaryPrimitives.ReadUInt16BigEndian(message.AsSpan()[1..3]);
        var displayName = ConvertAsciiBytesToString(message, 3);
        var messageContents = ConvertAsciiBytesToString(message, 3 + displayName.Length);

        return (messageId, displayName, messageContents);
    }

    private static string ConvertAsciiBytesToString(byte[] bytes, int startIndex)
    {
        byte[] shortByteArray;
        if (bytes[startIndex] == 0x00)
        {
            startIndex++;
            shortByteArray = bytes[startIndex..];
        }
        else
        {
            shortByteArray = bytes[startIndex..];
        }
        var length = Array.IndexOf(shortByteArray, (byte)0x00);
        length = length == -1 ? shortByteArray.Length - 1 : length + 1; // TODO: Check if this is correct

        return TextEncoding.GetString(shortByteArray, 0, length).Trim('\0');
    }
}