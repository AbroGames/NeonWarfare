using System.Buffers;
using GdUnit4;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Game.Transport;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game.Transport;

[TestSuite]
public class ServerPacketsTests
{
    private static readonly byte[] Body = [5, 6];

    [TestCase]
    [RequireGodotRuntime]
    public void Write_IsTheKindThenTheBody()
    {
        AssertThat(Write(ServerPacketKind.Events, Body)).ContainsExactly((byte) ServerPacketKind.Events, 5, 6);
    }

    // A client of another build reads the rejection of a protocol mismatch
    [TestCase]
    [RequireGodotRuntime]
    public void WriteJoinRejected_IsTheKindThenTheReason()
    {
        var output = new ArrayBufferWriter<byte>();
        ServerPackets.WriteJoinRejected(JoinRejectReason.InvalidNick, output);

        AssertThat(output.WrittenSpan.ToArray()).ContainsExactly((byte) ServerPacketKind.JoinRejected, 3);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_OfWrite_IsTheKindAndTheBody()
    {
        foreach (ServerPacketKind kind in Enum.GetValues<ServerPacketKind>())
        {
            (ServerPacketKind readKind, ReadOnlyMemory<byte> body) = ServerPackets.Read(Write(kind, Body));

            AssertThat(readKind).IsEqual(kind);
            AssertThat(body.ToArray()).ContainsExactly(Body);
        }
    }

    // A server of another build may send a code this one does not know: it is still a rejection
    [TestCase]
    [RequireGodotRuntime]
    public void ReadJoinRejected_OfWrite_IsTheReason_AnUnknownOneToo()
    {
        foreach (JoinRejectReason reason in Enum.GetValues<JoinRejectReason>().Append((JoinRejectReason) 200))
        {
            var output = new ArrayBufferWriter<byte>();
            ServerPackets.WriteJoinRejected(reason, output);
            (_, ReadOnlyMemory<byte> body) = ServerPackets.Read(output.WrittenMemory);

            AssertThat(ServerPackets.ReadJoinRejected(body.Span)).IsEqual(reason);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_EmptyOrUnknownKind_Throws()
    {
        foreach (byte[] packet in new byte[][] { [], [0], [5, 1] })
        {
            NetMessageCodecTests.AssertRejected(() => ServerPackets.Read(packet));
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadJoinRejected_NotOneByte_Throws()
    {
        foreach (byte[] body in new byte[][] { [], [1, 1] })
        {
            NetMessageCodecTests.AssertRejected(() => ServerPackets.ReadJoinRejected(body));
        }
    }

    private static byte[] Write(ServerPacketKind kind, byte[] body)
    {
        var output = new ArrayBufferWriter<byte>();
        ServerPackets.Write(kind, body, output);
        return output.WrittenSpan.ToArray();
    }
}
