using GdUnit4;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Protocol;

[TestSuite]
public class JoinRejectedPacketTests
{
    // A client of another build reads it, so the bytes themselves are the contract
    [TestCase]
    [RequireGodotRuntime]
    public void Write_IsTheKindThenTheReason()
    {
        AssertThat(JoinRejectedPacket.Write(JoinRejectReason.InvalidNick))
            .ContainsExactly((byte) ServerPacketKind.JoinRejected, (byte) 3);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_OfWrite_IsTheReason()
    {
        foreach (JoinRejectReason reason in Enum.GetValues<JoinRejectReason>())
        {
            AssertThat(JoinRejectedPacket.Read(JoinRejectedPacket.Write(reason))).IsEqual(reason);
        }
    }

    // A server of another build may send a code this one does not know: it is still a rejection
    [TestCase]
    [RequireGodotRuntime]
    public void Read_UnknownReason_IsReadAsIs()
    {
        AssertThat(JoinRejectedPacket.Read([(byte) ServerPacketKind.JoinRejected, 200]))
            .IsEqual((JoinRejectReason) 200);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_BrokenPacket_Throws()
    {
        foreach (byte[] packet in new[]
                 {
                     [], [(byte) ServerPacketKind.JoinRejected],
                     new[] { (byte) ServerPacketKind.JoinRejected, (byte) 1, (byte) 1 },
                     new[] { (byte) ServerPacketKind.Events, (byte) 1 },
                 })
        {
            AssertThrown(() => JoinRejectedPacket.Read(packet)).IsInstanceOf<NetMessageFormatException>();
        }
    }
}
