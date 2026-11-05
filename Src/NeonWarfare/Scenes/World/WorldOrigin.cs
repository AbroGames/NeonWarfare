using System;

namespace NeonWarfare.Scenes.World;

/// <summary>
/// Where the state of a World comes from. A server World also learns here the save file it saves to.
/// </summary>
public abstract record WorldOrigin
{
    private WorldOrigin() { }

    /// <summary>A world created from nothing, on the server.</summary>
    public sealed record NewWorld(string SaveFileName) : WorldOrigin;

    /// <summary>The world of a remote client, from the snapshot the server sends it on joining.</summary>
    public sealed record FromSnapshot(ReadOnlyMemory<byte> Packet) : WorldOrigin;

    /// <summary>A world loaded from a save, on the server: it saves back to the same file.</summary>
    public sealed record FromSave(ReadOnlyMemory<byte> Save, string SaveFileName) : WorldOrigin;
}
