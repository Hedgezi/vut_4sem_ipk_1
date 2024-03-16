using System.Buffers.Binary;
using System.Text;

namespace vut_ipk1.UdpClient.Messages;

public class UdpMessageParser
{
    private static readonly Encoding TextEncoding = Encoding.ASCII;

    public static (ushort messageId, bool result, ushort refMessageId, string messageContents) ParseReplyMessage(
        byte[] message)
    {
        var messageId = BinaryPrimitives.ReadUInt16LittleEndian(message.AsSpan()[1..3]);
        var result = message[3] == 0x01;
        var refMessageId = BinaryPrimitives.ReadUInt16LittleEndian(message.AsSpan()[4..6]);
        var messageContents = ConvertAsciiBytesToString(message, 6);

        return (messageId, result, refMessageId, messageContents);
    }

    public static (ushort messageId, string displayName, string messageContents) ParseMsgMessage(byte[] message)
    {
        var messageId = BinaryPrimitives.ReadUInt16LittleEndian(message.AsSpan()[1..3]);
        var displayName = ConvertAsciiBytesToString(message, 3);
        var messageContents = ConvertAsciiBytesToString(message, 3 + displayName.Length);

        return (messageId, displayName, messageContents);
    }

    private static string ConvertAsciiBytesToString(byte[] bytes, int startIndex)
    {
        var shortByteArray = bytes[startIndex..];
        var length = Array.IndexOf(shortByteArray, (byte)0x00);
        length = length == -1 ? shortByteArray.Length - 1 : length + 1;

        return TextEncoding.GetString(shortByteArray, 0, length);
    }
}