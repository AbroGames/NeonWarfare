using NeonWarfare.Scenes.Worlds.Infra.Tick;
using RepliCAT;
using RepliCAT.Bits;
using RepliCAT.Codecs;

namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>
/// Replicates a <see cref="TickTimer"/> as two varuints, so a running timer costs nothing until it is started again.
/// </summary>
public sealed class TickTimerCodec : IReplicationCodec<TickTimer>
{
    public static readonly TickTimerCodec Instance = new();

    public void Write(BitWriter writer, TickTimer value)
    {
        writer.WriteVarUInt((ulong) value.StartTick);
        writer.WriteVarUInt((ulong) value.DurationTicks);
    }

    public TickTimer Read(ref BitReader reader)
    {
        ulong start = reader.ReadVarUInt();
        ulong duration = reader.ReadVarUInt();
        if (start > long.MaxValue || duration > int.MaxValue || !TickTimer.IsValid((long) start, (long) duration))
        {
            throw new ReplicationFormatException($"TickTimer({start}, {duration}) is out of range.");
        }

        return new TickTimer((long) start, (int) duration);
    }

    public bool IsChanged(TickTimer lastSent, TickTimer current) => lastSent != current;
}
