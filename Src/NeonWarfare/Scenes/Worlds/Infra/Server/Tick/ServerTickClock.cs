using System;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Tick;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Tick;

/// <summary>
/// The tick counter of a server World.
/// Kept apart from <see cref="ServerTickLoop"/>, so the saves reach it without reaching the tick loop and,
/// through it, the command handlers.
/// </summary>
[Server]
public class ServerTickClock : IWorldClock
{
    /// <inheritdoc/>
    /// <remarks>Before the first tick it is 0 in a new world and the saved tick in a loaded one.</remarks>
    public long CurrentTick { get; private set; }

    /// <summary>
    /// Whether a tick has run in this World. Before the first tick the baselines hold nothing to save.
    /// </summary>
    public bool Started { get; private set; }

    /// <summary>
    /// Continues the tick counter of a loaded world. The first tick after the load is <paramref name="tick"/> + 1.
    /// </summary>
    public void Restore(long tick)
    {
        if (Started)
        {
            throw new InvalidOperationException(
                $"The tick counter is {CurrentTick}: a restore comes before the first tick.");
        }
        ArgumentOutOfRangeException.ThrowIfNegative(tick);

        CurrentTick = tick;
    }

    public void StartTick()
    {
        Started = true;
        CurrentTick++;
    }
}
