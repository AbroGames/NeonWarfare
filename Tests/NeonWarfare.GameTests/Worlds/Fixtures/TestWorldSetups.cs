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
    public static readonly SaveFileName SaveFileName = new("save");

    public static LocalPlayer LocalPlayer(PlayerUid? uid = null) => new(uid ?? LocalPlayerUid, "Local", Colors.White);

    public static WorldSetup.RemoteClient RemoteClient(
        LocalPlayer? localPlayer = null, ILocalPlayerOwner? localPlayerOwner = null) =>
        new(localPlayer ?? LocalPlayer(), localPlayerOwner ?? new RecordingLocalPlayerOwner());

    public static WorldSetup.Host Host(
        LocalPlayer? localPlayer = null, ISaveFiles? saveFiles = null, IServerOwner? owner = null,
        ILocalPlayerOwner? localPlayerOwner = null, SaveFileName? saveFileName = null) =>
        new(saveFiles ?? new RecordingSaveFiles(),
            new WorldSaving(saveFileName ?? SaveFileName, autoSaveIntervalMinutes: 0),
            localPlayer ?? LocalPlayer(),
            owner ?? new RecordingServerOwner(), localPlayerOwner ?? new RecordingLocalPlayerOwner());

    /// <param name="adminUid"><c>null</c> — a World without an admin.</param>
    /// <param name="protectedUid"><c>null</c> — a server started from the console, which displaces everyone.</param>
    /// <param name="autoSaveIntervalMinutes">Off by default, so a test running many ticks writes no save.</param>
    public static WorldSetup.Dedicated Dedicated(
        PlayerUid? adminUid = null, ISaveFiles? saveFiles = null, IServerOwner? owner = null,
        SaveFileName? saveFileName = null, PlayerUid? protectedUid = null, int autoSaveIntervalMinutes = 0) =>
        new(saveFiles ?? new RecordingSaveFiles(),
            new WorldSaving(saveFileName ?? SaveFileName, autoSaveIntervalMinutes),
            new WorldAdmin(adminUid),
            new ProtectedPlayer(protectedUid), owner ?? new RecordingServerOwner());
}
