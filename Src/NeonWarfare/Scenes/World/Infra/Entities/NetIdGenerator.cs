using NeonWarfare.Scenes.World.Infra.Composition;


namespace NeonWarfare.Scenes.World.Infra.Entities;

/// <summary>
/// Hands out <see cref="NetId"/>s on the server: 1, 2, … in order, never <see cref="NetId.None"/>, never twice.
/// </summary>
[ServerNetwork]
public class NetIdGenerator
{
    private long _next = 1;

    /// <summary>The value the next <see cref="Next"/> returns.</summary>
    public long NextValue => _next;

    public NetId Next() => new(_next++);
}
