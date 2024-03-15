using System.Text;

namespace vut_ipk1.UdpClient.Messages;

public class UdpMessageParser
{
    private static readonly Encoding TextEncoding = Encoding.ASCII;

    public static (ushort messageId, bool result, ushort refMessageId, string messageContents) ParseReplyMessage(
        byte[] message)
    {
        var messageId = BitConverter.ToUInt16(message, 1);
        var result = message[3] == 0x01;
        var refMessageId = BitConverter.ToUInt16(message, 4);
        var messageContents = ConvertAsciiBytesToString(message, 6);

        return (messageId, result, refMessageId, messageContents);
    }

    public static (ushort messageId, string displayName, string messageContents) ParseMsgMessage(byte[] message)
    {
        var messageId = BitConverter.ToUInt16(message, 1);
        var displayName = ConvertAsciiBytesToString(message, 3);
        var messageContents = ConvertAsciiBytesToString(message, 3 + displayName.Length);

        return (messageId, displayName, messageContents);
    }

    private static string ConvertAsciiBytesToString(byte[] bytes, int startIndex)
    {
        // TODO: rework
        var length = 0;
        while (bytes[startIndex + length] != 0)
        {
            length++;
        }

        return TextEncoding.GetString(bytes, startIndex, length);
    }
}