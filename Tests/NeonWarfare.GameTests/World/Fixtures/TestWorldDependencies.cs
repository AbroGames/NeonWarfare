using Godot;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;

namespace NeonWarfare.GameTests.World.Fixtures;

/// <summary>
/// The layer-dependent members of <c>WorldDependencies</c> a World of these layers is built with.
/// </summary>
public static class TestWorldDependencies
{
    public const string LocalPlayerUid = "LocalLocal-Llllllllll";

    /// <returns><c>null</c> on a dedicated server.</returns>
    public static LocalPlayer? LocalPlayer(WorldLayer layers) =>
        layers.HasFlag(WorldLayer.Presentation) ? new LocalPlayer(LocalPlayerUid, "Local", Colors.White) : null;

    /// <returns><c>null</c> on a remote client.</returns>
    public static WorldAdmin? Admin(WorldLayer layers, string? uid = null) =>
        layers.HasFlag(WorldLayer.Simulation) ? new WorldAdmin(uid) : null;

    /// <returns><c>null</c> except on a dedicated server.</returns>
    public static IDedicatedServerOwner? DedicatedServerOwner(WorldLayer layers) =>
        layers.HasFlag(WorldLayer.Simulation) && !layers.HasFlag(WorldLayer.Presentation)
            ? new RecordingDedicatedServerOwner()
            : null;

    /// <returns><c>null</c> on a dedicated server.</returns>
    public static ILocalPlayerOwner? LocalPlayerOwner(WorldLayer layers) =>
        layers.HasFlag(WorldLayer.Presentation) ? new RecordingLocalPlayerOwner() : null;
}
