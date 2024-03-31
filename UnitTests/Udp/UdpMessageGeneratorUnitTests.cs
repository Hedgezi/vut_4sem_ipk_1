using vut_ipk1.Common.Enums;
using Xunit;
using vut_ipk1.Udp.Messages;

namespace UnitTests.Udp;

public class UdpMessageGeneratorUnitTest
{
    [Fact]
    public void ConfirmMessageTest()
    {
        var expected = new byte[] { (byte)MessageType.CONFIRM, 0x12, 0x34};

        Assert.Equal(expected, UdpMessageGenerator.GenerateConfirmMessage(0x1234));
    }
    
    [Fact]
    public void AuthMessageTest()
    {
        var id = (ushort)1;
        var username = "username";
        var displayName = "displayName";
        var secret = "secret";

        var expectedMessage = new byte[]
        {
            (byte)MessageType.AUTH,
            0, 1,
            (byte)'u', (byte)'s', (byte)'e', (byte)'r', (byte)'n', (byte)'a', (byte)'m', (byte)'e', 0,
            (byte)'d', (byte)'i', (byte)'s', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'N', (byte)'a', (byte)'m', (byte)'e', 0,
            (byte)'s', (byte)'e', (byte)'c', (byte)'r', (byte)'e', (byte)'t', 0
        };

        var actualMessage = UdpMessageGenerator.GenerateAuthMessage(id, username, displayName, secret);

        Assert.Equal(expectedMessage, actualMessage);
    }

    [Fact]
    public void JoinMessageTest()
    {
        var id = (ushort)1;
        var channelId = "channelId";
        var displayName = "displayName";

        var expectedMessage = new byte[]
        {
            (byte)MessageType.JOIN,
            0, 1,
            (byte)'c', (byte)'h', (byte)'a', (byte)'n', (byte)'n', (byte)'e', (byte)'l', (byte)'I', (byte)'d', 0,
            (byte)'d', (byte)'i', (byte)'s', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'N', (byte)'a', (byte)'m', (byte)'e', 0
        };

        var actualMessage = UdpMessageGenerator.GenerateJoinMessage(id, channelId, displayName);

        Assert.Equal(expectedMessage, actualMessage);
    }

    [Fact]
    public void MsgMessageTest()
    {
        var id = (ushort)1;
        var displayName = "displayName";
        var contents = "contents";

        var expectedMessage = new byte[]
        {
            (byte)MessageType.MSG,
            0, 1,
            (byte)'d', (byte)'i', (byte)'s', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'N', (byte)'a', (byte)'m', (byte)'e', 0,
            (byte)'c', (byte)'o', (byte)'n', (byte)'t', (byte)'e', (byte)'n', (byte)'t', (byte)'s', 0
        };

        var actualMessage = UdpMessageGenerator.GenerateMsgMessage(id, displayName, contents);

        Assert.Equal(expectedMessage, actualMessage);
    }

    [Fact]
    public void ErrMessageTest()
    {
        var id = (ushort)1;
        var displayName = "displayName";
        var contents = "contents";

        var expectedMessage = new byte[]
        {
            (byte)MessageType.ERR,
            0, 1,
            (byte)'d', (byte)'i', (byte)'s', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'N', (byte)'a', (byte)'m', (byte)'e', 0,
            (byte)'c', (byte)'o', (byte)'n', (byte)'t', (byte)'e', (byte)'n', (byte)'t', (byte)'s', 0
        };

        var actualMessage = UdpMessageGenerator.GenerateErrMessage(id, displayName, contents);

        Assert.Equal(expectedMessage, actualMessage);
    }

    [Fact]
    public void ByeMessageTest()
    {
        var id = (ushort)1;

        var expectedMessage = new byte[]
        {
            (byte)MessageType.BYE,
            0, 1
        };

        var actualMessage = UdpMessageGenerator.GenerateByeMessage(id);

        Assert.Equal(expectedMessage, actualMessage);
    }
}