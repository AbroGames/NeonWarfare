using System;
using NeonWarfare.Scenes.Worlds.Features.Options;

namespace NeonWarfare.Scenes.Worlds;

/// <summary>
/// Where the state of a World comes from.
/// </summary>
public abstract record WorldOrigin
{
    private WorldOrigin() { }

    /// <summary>A world created from nothing, on the server.</summary>
    public sealed record NewWorld(WorldOptions Options) : WorldOrigin;

    /// <summary>The world of a remote client, from the snapshot the server sends it on joining.</summary>
    public sealed record FromSnapshot(long Tick, ReadOnlyMemory<byte> Records) : WorldOrigin;

    /// <summary>A world loaded from a save on the server.</summary>
    public sealed record FromSave(SaveFileName FileName, ReadOnlyMemory<byte> Save) : WorldOrigin;
}
