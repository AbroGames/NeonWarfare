using System;
using System.Buffers;
using System.Buffers.Binary;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Game.Networks.Transport;

/// <summary>
/// The layout of a server packet: the <see cref="ServerPacketKind"/> byte, then the body.
/// Integers are little-endian.
/// <list type="bullet">
/// <item><see cref="ServerPacketKind.Snapshot"/>: the tick (<c>long</c>), then the records to the end.</item>
/// <item><see cref="ServerPacketKind.TickUpdate"/>: the tick (<c>long</c>), the length of the state section
/// (<c>int</c>), the state section, then the events section to the end. A section is absent when it is empty, and at
/// least one is present.</item>
/// <item><see cref="ServerPacketKind.JoinRejected"/>: one <see cref="JoinRejectReason"/> byte.</item>
/// </list>
/// </summary>
public static class ServerPackets
{
    private const int TickSize = sizeof(long);
    private const int StateLengthSize = sizeof(int);

    /// <exception cref="ArgumentException">Both sections are empty.</exception>
    public static void WriteTickUpdate(
        long tick, ReadOnlySpan<byte> state, ReadOnlySpan<byte> events, IBufferWriter<byte> output)
    {
        if (state.IsEmpty && events.IsEmpty)
        {
            throw new ArgumentException("A tick update carries a state section, an events section or both.");
        }

        int length = 1 + TickSize + StateLengthSize + state.Length + events.Length;
        Span<byte> span = output.GetSpan(length);
        span[0] = (byte) ServerPacketKind.TickUpdate;
        BinaryPrimitives.WriteInt64LittleEndian(span[1..], tick);
        BinaryPrimitives.WriteInt32LittleEndian(span[(1 + TickSize)..], state.Length);
        state.CopyTo(span[(1 + TickSize + StateLengthSize)..]);
        events.CopyTo(span[(1 + TickSize + StateLengthSize + state.Length)..]);
        output.Advance(length);
    }

    public static void WriteSnapshot(long tick, ReadOnlySpan<byte> records, IBufferWriter<byte> output)
    {
        int length = 1 + TickSize + records.Length;
        Span<byte> span = output.GetSpan(length);
        span[0] = (byte) ServerPacketKind.Snapshot;
        BinaryPrimitives.WriteInt64LittleEndian(span[1..], tick);
        records.CopyTo(span[(1 + TickSize)..]);
        output.Advance(length);
    }

    public static void WriteJoinRejected(JoinRejectReason reason, IBufferWriter<byte> output)
    {
        Span<byte> span = output.GetSpan(2);
        span[0] = (byte) ServerPacketKind.JoinRejected;
        span[1] = (byte) reason;
        output.Advance(2);
    }

    /// <exception cref="NetMessageFormatException">The packet is empty or of an unknown kind.</exception>
    public static (ServerPacketKind Kind, ReadOnlyMemory<byte> Body) Read(ReadOnlyMemory<byte> packet)
    {
        if (packet.IsEmpty)
        {
            throw new NetMessageFormatException("The packet is empty.");
        }

        var kind = (ServerPacketKind) packet.Span[0];
        if (!Enum.IsDefined(kind))
        {
            throw new NetMessageFormatException($"Unknown server packet kind {packet.Span[0]}.");
        }
        return (kind, packet[1..]);
    }

    /// <summary>The sections are not read, only located: an empty one is absent.</summary>
    /// <exception cref="NetMessageFormatException">
    /// The header is truncated, the tick or the state length is impossible, or both sections are absent.
    /// </exception>
    public static (long Tick, ReadOnlyMemory<byte> State, ReadOnlyMemory<byte> Events) ReadTickUpdate(
        ReadOnlyMemory<byte> body)
    {
        if (body.Length < TickSize + StateLengthSize)
        {
            throw new NetMessageFormatException(
                $"A tick update body is {body.Length} bytes long, shorter than its header.");
        }

        long tick = ReadTick(body.Span);
        int stateLength = BinaryPrimitives.ReadInt32LittleEndian(body.Span[TickSize..]);
        ReadOnlyMemory<byte> sections = body[(TickSize + StateLengthSize)..];
        if (stateLength < 0 || stateLength > sections.Length)
        {
            throw new NetMessageFormatException(
                $"A tick update claims a state section of {stateLength} bytes, but {sections.Length} follow.");
        }
        if (sections.IsEmpty)
        {
            throw new NetMessageFormatException("A tick update carries neither a state section nor events.");
        }
        return (tick, sections[..stateLength], sections[stateLength..]);
    }

    /// <exception cref="NetMessageFormatException">The tick is truncated or negative.</exception>
    public static (long Tick, ReadOnlyMemory<byte> Records) ReadSnapshot(ReadOnlyMemory<byte> body)
    {
        if (body.Length < TickSize)
        {
            throw new NetMessageFormatException(
                $"A snapshot body is {body.Length} bytes long, shorter than its tick.");
        }
        return (ReadTick(body.Span), body[TickSize..]);
    }

    /// <summary>A reason this build does not know is read as it is, because it is still a rejection.</summary>
    /// <exception cref="NetMessageFormatException">The body is not one byte long.</exception>
    public static JoinRejectReason ReadJoinRejected(ReadOnlySpan<byte> body)
    {
        if (body.Length != 1)
        {
            throw new NetMessageFormatException($"A join rejection body is {body.Length} bytes long, 1 expected.");
        }
        return (JoinRejectReason) body[0];
    }

    private static long ReadTick(ReadOnlySpan<byte> body)
    {
        long tick = BinaryPrimitives.ReadInt64LittleEndian(body);
        if (tick < 0)
        {
            throw new NetMessageFormatException($"The tick {tick} is negative.");
        }
        return tick;
    }
}
