using Godot;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.GameTests.Worlds.Fixtures;

/// <summary>
/// The configurations a test World is built with; what a test does not pass is a fresh fake.
/// </summary>
public static class TestWorldSetups
{
    public const string LocalPlayerUid = "LocalLocal-Llllllllll";

    public static LocalPlayer LocalPlayer(string uid = LocalPlayerUid) => new(uid, "Local", Colors.White);

    public static WorldSetup.RemoteClient RemoteClient(
        LocalPlayer? localPlayer = null, ILocalPlayerOwner? localPlayerOwner = null) =>
        new(localPlayer ?? LocalPlayer(), localPlayerOwner ?? new RecordingLocalPlayerOwner());

    public static WorldSetup.Host Host(
        LocalPlayer? localPlayer = null, ISaveFiles? saveFiles = null, IServerOwner? owner = null,
        ILocalPlayerOwner? localPlayerOwner = null) =>
        new(saveFiles ?? new RecordingSaveFiles(), localPlayer ?? LocalPlayer(), owner ?? new RecordingServerOwner(),
            localPlayerOwner ?? new RecordingLocalPlayerOwner());

    /// <param name="adminUid"><c>null</c> — a World without an admin.</param>
    public static WorldSetup.Dedicated Dedicated(
        string? adminUid = null, ISaveFiles? saveFiles = null, IServerOwner? owner = null) =>
        new(saveFiles ?? new RecordingSaveFiles(), new WorldAdmin(adminUid), owner ?? new RecordingServerOwner());
}
