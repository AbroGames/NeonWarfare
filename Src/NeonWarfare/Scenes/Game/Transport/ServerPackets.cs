using System;
using System.Buffers;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Game.Transport;

/// <summary>
/// The layout of a server packet: the <see cref="ServerPacketKind"/> byte, then the body the World wrote. The body of
/// <see cref="ServerPacketKind.JoinRejected"/> is one <see cref="JoinRejectReason"/> byte.
/// </summary>
public static class ServerPackets
{
    private const string EmptyPacketError = "The packet is empty.";
    private const string UnknownKindError = "Unknown server packet kind {0}.";
    private const string JoinRejectedLengthError = "A join rejection body is {0} bytes long, 1 expected.";

    public static void Write(ServerPacketKind kind, ReadOnlySpan<byte> body, IBufferWriter<byte> output)
    {
        Span<byte> span = output.GetSpan(1 + body.Length);
        span[0] = (byte) kind;
        body.CopyTo(span[1..]);
        output.Advance(1 + body.Length);
    }

    public static void WriteJoinRejected(JoinRejectReason reason, IBufferWriter<byte> output) =>
        Write(ServerPacketKind.JoinRejected, [(byte) reason], output);

    /// <exception cref="NetMessageFormatException">The packet is empty or of an unknown kind.</exception>
    public static (ServerPacketKind Kind, ReadOnlyMemory<byte> Body) Read(ReadOnlyMemory<byte> packet)
    {
        if (packet.IsEmpty)
        {
            throw new NetMessageFormatException(EmptyPacketError);
        }

        var kind = (ServerPacketKind) packet.Span[0];
        if (!Enum.IsDefined(kind))
        {
            throw new NetMessageFormatException(UnknownKindError.FormatWith(packet.Span[0]));
        }
        return (kind, packet[1..]);
    }

    /// <summary>
    /// A reason this build does not know is read as is: it is still a rejection.
    /// </summary>
    /// <exception cref="NetMessageFormatException">The body is not one byte long.</exception>
    public static JoinRejectReason ReadJoinRejected(ReadOnlySpan<byte> body)
    {
        if (body.Length != 1)
        {
            throw new NetMessageFormatException(JoinRejectedLengthError.FormatWith(body.Length));
        }
        return (JoinRejectReason) body[0];
    }
}
