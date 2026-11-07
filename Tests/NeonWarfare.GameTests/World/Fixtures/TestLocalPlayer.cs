using Godot;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.GameTests.World.Fixtures;

/// <summary>
/// The local player a World of these layers is built with: one for a World with a Presentation, none for a
/// dedicated server.
/// </summary>
public static class TestLocalPlayer
{
    public const string Uid = "LocalLocal-Llllllllll";

    public static LocalPlayer? For(WorldLayer layers) =>
        layers.HasFlag(WorldLayer.Presentation) ? new LocalPlayer(Uid, "Local", Colors.White) : null;
}
