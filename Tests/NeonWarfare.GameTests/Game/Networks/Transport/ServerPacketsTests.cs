using System.Buffers;
using GdUnit4;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Game.Networks.Transport;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game.Networks.Transport;

[TestSuite]
public class ServerPacketsTests
{
    private static readonly byte[] Body = [5, 6];
    private static readonly byte[] Events = [7];
    private const long Tick = 0x0102;

    // The wire format: the kind, the tick and the state length little-endian, then the sections
    [TestCase]
    [RequireGodotRuntime]
    public void WriteTickUpdate_IsTheKindTheTickTheStateLengthThenTheSections()
    {
        AssertThat(WriteTickUpdate(Tick, Body, Events)).ContainsExactly(
            (byte) ServerPacketKind.TickUpdate, 2, 1, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 5, 6, 7);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WriteSnapshot_IsTheKindTheTickThenTheRecords()
    {
        var output = new ArrayBufferWriter<byte>();
        ServerPackets.WriteSnapshot(Tick, Body, output);

        AssertThat(output.WrittenSpan.ToArray())
            .ContainsExactly((byte) ServerPacketKind.Snapshot, 2, 1, 0, 0, 0, 0, 0, 0, 5, 6);
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
    public void Read_IsTheKindAndTheBody()
    {
        foreach (ServerPacketKind kind in Enum.GetValues<ServerPacketKind>())
        {
            (ServerPacketKind readKind, ReadOnlyMemory<byte> body) =
                ServerPackets.Read(new byte[] { (byte) kind, 5, 6 });

            AssertThat(readKind).IsEqual(kind);
            AssertThat(body.ToArray()).ContainsExactly(Body);
        }
    }

    // An absent section reads back empty
    [TestCase]
    [RequireGodotRuntime]
    public void ReadTickUpdate_OfWrite_IsTheTickAndTheSections()
    {
        foreach ((byte[] state, byte[] events) in new (byte[], byte[])[] { (Body, []), ([], Events), (Body, Events) })
        {
            (long tick, ReadOnlyMemory<byte> readState, ReadOnlyMemory<byte> readEvents) =
                ServerPackets.ReadTickUpdate(BodyOf(WriteTickUpdate(Tick, state, events)));

            AssertThat(tick).IsEqual(Tick);
            AssertThat(readState.ToArray()).ContainsExactly(state);
            AssertThat(readEvents.ToArray()).ContainsExactly(events);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WriteTickUpdate_NeitherSection_Throws()
    {
        AssertThrown(() => WriteTickUpdate(Tick, [], [])).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadTickUpdate_BrokenHeader_Throws()
    {
        byte[] truncated = [2, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0];
        byte[] negativeTick = [255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0, 7];
        byte[] negativeStateLength = [2, 1, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 7];
        byte[] stateLengthPastTheEnd = [2, 1, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 7];
        byte[] neitherSection = [2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        foreach (byte[] body in
                 new[] { truncated, negativeTick, negativeStateLength, stateLengthPastTheEnd, neitherSection })
        {
            NetMessageCodecTests.AssertRejected(() => ServerPackets.ReadTickUpdate(body));
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadSnapshot_OfWrite_IsTheTickAndTheRecords()
    {
        var output = new ArrayBufferWriter<byte>();
        ServerPackets.WriteSnapshot(Tick, Body, output);

        (long tick, ReadOnlyMemory<byte> records) = ServerPackets.ReadSnapshot(BodyOf(output.WrittenSpan.ToArray()));

        AssertThat(tick).IsEqual(Tick);
        AssertThat(records.ToArray()).ContainsExactly(Body);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ReadSnapshot_TruncatedOrNegativeTick_Throws()
    {
        foreach (byte[] body in new byte[][] { [2, 1, 0, 0, 0, 0, 0], [255, 255, 255, 255, 255, 255, 255, 255, 5] })
        {
            NetMessageCodecTests.AssertRejected(() => ServerPackets.ReadSnapshot(body));
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
        foreach (byte[] packet in new byte[][] { [], [0], [2, 1], [3, 1], [6, 1] })
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

    private static byte[] WriteTickUpdate(long tick, byte[] state, byte[] events)
    {
        var output = new ArrayBufferWriter<byte>();
        ServerPackets.WriteTickUpdate(tick, state, events, output);
        return output.WrittenSpan.ToArray();
    }

    private static ReadOnlyMemory<byte> BodyOf(byte[] packet) => ServerPackets.Read(packet).Body;
}
