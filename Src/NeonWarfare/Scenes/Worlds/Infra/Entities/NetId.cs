namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// The network identity of an entity: the same on the server and on every client, never reused within a world.
/// </summary>
public readonly record struct NetId(long Value)
{
    public static readonly NetId None = default;

    public override string ToString() => $"NetId({Value})";
}
