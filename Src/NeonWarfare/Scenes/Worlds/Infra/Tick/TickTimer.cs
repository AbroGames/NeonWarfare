using System;

namespace NeonWarfare.Scenes.Worlds.Infra.Tick;

/// <summary>
/// A span of server ticks, read against the current tick of <see cref="IWorldClock"/>. <c>default</c> is finished.
/// </summary>
public readonly struct TickTimer : IEquatable<TickTimer>
{
    public long StartTick { get; }
    public int DurationTicks { get; }
    public long EndTick => StartTick + DurationTicks;

    /// <summary>Takes values already checked by <see cref="IsValid"/>.</summary>
    internal TickTimer(long startTick, int durationTicks)
    {
        StartTick = startTick;
        DurationTicks = durationTicks;
    }

    public static TickTimer Start(long currentTick, int durationTicks)
    {
        if (!IsValid(currentTick, durationTicks))
        {
            throw new ArgumentOutOfRangeException(
                currentTick < 0 ? nameof(currentTick) : nameof(durationTicks),
                $"Cannot start a timer of {durationTicks} ticks at tick {currentTick}.");
        }

        return new TickTimer(currentTick, durationTicks);
    }

    internal static bool IsValid(long startTick, long durationTicks) =>
        startTick >= 0 && durationTicks is >= 0 and <= int.MaxValue && startTick <= long.MaxValue - durationTicks;

    public long Elapsed(long currentTick) => Math.Clamp(currentTick - StartTick, 0, DurationTicks);

    public long Remaining(long currentTick) => DurationTicks - Elapsed(currentTick);

    public bool IsFinished(long currentTick) => currentTick >= EndTick;

    /// <summary>From 0 to 1; a zero duration is 1 at once.</summary>
    public double Progress(long currentTick) => DurationTicks == 0 ? 1 : (double) Elapsed(currentTick) / DurationTicks;

    public bool Equals(TickTimer other) => StartTick == other.StartTick && DurationTicks == other.DurationTicks;

    public override bool Equals(object obj) => obj is TickTimer other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(StartTick, DurationTicks);

    public static bool operator ==(TickTimer left, TickTimer right) => left.Equals(right);

    public static bool operator !=(TickTimer left, TickTimer right) => !left.Equals(right);

    public override string ToString() => $"TickTimer({StartTick}, {DurationTicks})";
}
