using System;
using NeonWarfare.Scenes.Worlds.Infra.Composition;

namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// Hands out <see cref="NetId"/>s on the server in order: 1, 2, and so on.
/// Never <see cref="NetId.None"/>, never the same value twice.
/// </summary>
[Server]
public class NetIdGenerator
{
    private long _next = 1;

    /// <summary>The value the next <see cref="Next"/> returns.</summary>
    public long NextValue => _next;

    public NetId Next() => new(_next++);

    /// <summary>
    /// Continues the sequence of a loaded world, so a new entity never takes the NetId of a saved one.
    /// </summary>
    public void Restore(long next)
    {
        if (_next != 1)
        {
            throw new InvalidOperationException(
                $"{_next - 1} NetIds are already handed out: a restore comes before the first.");
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(next, 1);

        _next = next;
    }
}
