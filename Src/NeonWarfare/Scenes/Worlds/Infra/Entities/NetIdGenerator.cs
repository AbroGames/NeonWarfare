using System;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Infra.Composition;


namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// Hands out <see cref="NetId"/>s on the server: 1, 2, … in order, never <see cref="NetId.None"/>, never twice.
/// </summary>
[ServerNetwork]
public class NetIdGenerator
{
    private const string AlreadyUsedError = "{0} NetIds are already handed out: a restore comes before the first.";
    private const string NotPositiveError = "The next NetId is {0}, not positive.";

    private long _next = 1;

    /// <summary>The value the next <see cref="Next"/> returns.</summary>
    public long NextValue => _next;

    public NetId Next() => new(_next++);

    /// <summary>
    /// Continues the sequence of a loaded world, so a new entity never takes the NetId of a saved one.
    /// </summary>
    public void Restore(long next)
    {
        if (_next != 1) throw new InvalidOperationException(AlreadyUsedError.FormatWith(_next - 1));
        if (next < 1) throw new ArgumentOutOfRangeException(nameof(next), next, NotPositiveError.FormatWith(next));

        _next = next;
    }
}
