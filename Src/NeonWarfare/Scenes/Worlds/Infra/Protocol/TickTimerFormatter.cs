using MessagePack;
using MessagePack.Formatters;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Tick;

// The MessagePack source generator and analyzer accept TickTimer members of [MessagePackObject] types only with this.
[assembly: MessagePackKnownFormatter(typeof(TickTimerFormatter))]

namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>Writes a <see cref="TickTimer"/> as an array of its start tick and its duration.</summary>
public sealed class TickTimerFormatter : IMessagePackFormatter<TickTimer>
{
    public static readonly TickTimerFormatter Instance = new();

    public void Serialize(ref MessagePackWriter writer, TickTimer value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(2);
        writer.Write(value.StartTick);
        writer.Write(value.DurationTicks);
    }

    public TickTimer Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        int length = reader.ReadArrayHeader();
        if (length != 2)
        {
            throw new MessagePackSerializationException($"TickTimer is an array of 2, got {length}.");
        }

        long start = reader.ReadInt64();
        long duration = reader.ReadInt64();
        if (!TickTimer.IsValid(start, duration))
        {
            throw new MessagePackSerializationException($"TickTimer({start}, {duration}) is out of range.");
        }

        return new TickTimer(start, (int) duration);
    }
}
