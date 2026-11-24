using System;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Infra.Composition;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Tick;

/// <summary>
/// The tick counter of the World. Apart from <see cref="ServerTickLoop"/>, so the saves check and restore it without
/// reaching the tick loop and through it the command handlers.
/// </summary>
[Server]
public class ServerTickClock
{
    private const string RestoreAfterTickError = "The tick counter is {0}: a restore comes before the first tick.";
    private const string NegativeTickError = "Tick {0} is negative.";

    /// <summary>
    /// Grows at the start of a tick: inside tick N it is N, and it stays N until tick N + 1 starts.
    /// 0 before the first tick of a new world, the saved tick before the first tick of a loaded one.
    /// </summary>
    public long CurrentTick { get; private set; }

    /// <summary>
    /// Whether a tick has run in this World: before that its baselines hold nothing to save.
    /// </summary>
    public bool Started { get; private set; }

    /// <summary>
    /// Continues the tick counter of a loaded world: the first tick after it is <paramref name="tick"/> + 1.
    /// </summary>
    public void Restore(long tick)
    {
        if (Started) throw new InvalidOperationException(RestoreAfterTickError.FormatWith(CurrentTick));
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick), tick, NegativeTickError.FormatWith(tick));

        CurrentTick = tick;
    }

    // Calls from ServerTickLoop, first thing in a tick
    public void StartTick()
    {
        Started = true;
        CurrentTick++;
    }
}
