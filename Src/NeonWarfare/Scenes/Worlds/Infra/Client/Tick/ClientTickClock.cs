using System;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Tick;

namespace NeonWarfare.Scenes.Worlds.Infra.Client.Tick;

/// <summary>
/// The tick counter of a remote client, synced from the server's packets and advanced between them, see
/// Docs/Networking.md#the-client-tick.
/// </summary>
[ClientReplication]
public class ClientTickClock : IWorldClock
{
    // How many ticks the clock is ahead of the latest packet: so many next advances are skipped.
    private long _lead;

    /// <inheritdoc/>
    /// <remarks>Never goes back, so it is never less than the tick of any packet synced.</remarks>
    public long CurrentTick { get; private set; }

    /// <summary>
    /// Moves the clock forward to a tick ahead of it at once, and makes the clock skip as many next advances as it
    /// is ahead of a tick behind it.
    /// </summary>
    public void Sync(long tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tick);

        if (tick > CurrentTick) CurrentTick = tick;
        _lead = CurrentTick - tick;
    }

    public void Advance()
    {
        if (_lead > 0)
        {
            _lead--;
            return;
        }
        CurrentTick++;
    }
}
