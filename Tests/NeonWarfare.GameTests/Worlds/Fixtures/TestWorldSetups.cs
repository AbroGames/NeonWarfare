using Godot;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scripts;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.GameTests.Worlds.Fixtures;

/// <summary>
/// The configurations a test World is built with; what a test does not pass is a fresh fake.
/// </summary>
public static class TestWorldSetups
{
    public static readonly PlayerUid LocalPlayerUid = new("LocalLocal-Llllllllll");
    public const string SaveFileName = "save";

    public static LocalPlayer LocalPlayer(PlayerUid? uid = null) => new(uid ?? LocalPlayerUid, "Local", Colors.White);

    public static WorldSetup.RemoteClient RemoteClient(
        LocalPlayer? localPlayer = null, ILocalPlayerOwner? localPlayerOwner = null) =>
        new(localPlayer ?? LocalPlayer(), localPlayerOwner ?? new RecordingLocalPlayerOwner());

    public static WorldSetup.Host Host(
        LocalPlayer? localPlayer = null, ISaveFiles? saveFiles = null, IServerOwner? owner = null,
        ILocalPlayerOwner? localPlayerOwner = null, string saveFileName = SaveFileName) =>
        new(saveFiles ?? new RecordingSaveFiles(), new WorldSaveFile(saveFileName), localPlayer ?? LocalPlayer(),
            owner ?? new RecordingServerOwner(), localPlayerOwner ?? new RecordingLocalPlayerOwner());

    /// <param name="adminUid"><c>null</c> — a World without an admin.</param>
    public static WorldSetup.Dedicated Dedicated(
        PlayerUid? adminUid = null, ISaveFiles? saveFiles = null, IServerOwner? owner = null,
        string saveFileName = SaveFileName) =>
        new(saveFiles ?? new RecordingSaveFiles(), new WorldSaveFile(saveFileName), new WorldAdmin(adminUid),
            owner ?? new RecordingServerOwner());
}
