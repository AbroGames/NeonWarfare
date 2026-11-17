using System;
using Humanizer;

namespace NeonWarfare.Scenes.World.Infra.Protocol;

/// <summary>
/// The kind byte, then one <see cref="JoinRejectReason"/> byte. The layout never changes: the rejection of
/// <see cref="JoinRejectReason.ProtocolMismatch"/> is read by a client of another build.
/// </summary>
public static class JoinRejectedPacket
{
    private const int Length = 2;

    private const string LengthError = "A join rejection is {0} bytes long, {1} expected.";

    public static byte[] Write(JoinRejectReason reason) => [(byte) ServerPacketKind.JoinRejected, (byte) reason];

    /// <exception cref="NetMessageFormatException">The packet is not a join rejection of the right length.</exception>
    public static JoinRejectReason Read(ReadOnlySpan<byte> packet)
    {
        if (packet.Length != Length || packet[0] != (byte) ServerPacketKind.JoinRejected)
        {
            throw new NetMessageFormatException(LengthError.FormatWith(packet.Length, Length));
        }

        return (JoinRejectReason) packet[1];
    }
}
