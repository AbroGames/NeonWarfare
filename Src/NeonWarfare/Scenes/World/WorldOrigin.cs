using System;

namespace NeonWarfare.Scenes.World;

/// <summary>
/// Where the state of a World comes from
/// </summary>
public abstract record WorldOrigin
{
    public static readonly WorldOrigin New = new NewWorld();

    private WorldOrigin() { }

    /// <summary>A world created from nothing, on the server.</summary>
    public sealed record NewWorld : WorldOrigin;

    /// <summary>The world of a remote client, from the snapshot the server sends it on joining.</summary>
    public sealed record FromSnapshot(ReadOnlyMemory<byte> Packet) : WorldOrigin;

    /// <summary>A world loaded from a save, on the server.</summary>
    public sealed record FromSave(ReadOnlyMemory<byte> Save) : WorldOrigin;
}
