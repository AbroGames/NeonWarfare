using System;
using Godot;
using GodotBox.Godot.Nodes.Process;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;
using Serilog;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// A server without a player of its own.
/// </summary>
/// <param name="parentPid">The client that started this server as its child process; <c>null</c> for a server
/// started from the console, which outlives its admin and is never resumed by "Continue".</param>
public class DedicatedServerGameStarter(
    string saveFileName,
    int? port,
    string adminUid,
    int? parentPid,
    bool serverHud
    ) : BaseGameStarter
{
    private const string AdminLeftLog = "The admin of a server started by its client has left. Shutdown server.";

    private readonly ILogger _log = LogFactory.GetForStatic<DedicatedServerGameStarter>();

    public override void Start(Game game)
    {
        if (parentPid.HasValue)
        {
            var clientDeadChecker = new ProcessDeadChecker(
                parentPid.Value,
                () => Services.MainScene.Shutdown(),
                pid => $"Parent process {pid} is dead. Shutdown server.");
            game.AddChild(clientDeadChecker);
        }

        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        Network network = game.AddServerNetwork();
        ISaveFiles saveFiles = Services.SaveLoad;
        if (parentPid.HasValue)
        {
            ResumableGame lastGame =
                ResumableGame.GetCreateServer(saveFileName, port ?? DefaultPort, isDedicated: true);
            SetLastGame(lastGame);
            saveFiles = SaveFilesUpdatingLastGame(lastGame);
        }

        // A server without a port has nobody to serve; Network has already logged the error
        if (network.HostServer(port ?? DefaultPort) != Error.Ok)
        {
            Services.MainScene.Shutdown();
            return;
        }

        if (parentPid.HasValue)
        {
            // The client that started this server has left it: the server is stopped gracefully, so it saves on exit
            void OnAdminLeft()
            {
                _log.Information(AdminLeftLog);
                Services.MainScene.Shutdown();
            }

            game.AdminLeft += OnAdminLeft;
        }

        World world;
        try
        {
            world = game.AddDedicatedWorld(saveFiles, new WorldAdmin(adminUid), LoadServerOrigin(saveFileName));
        }
        catch (Exception e) when (IsLoadError(e))
        {
            // Starting a new world instead would overwrite the save on exit
            LogLoadError(e, saveFileName);
            Services.MainScene.Shutdown();
            return;
        }

        world.SetVisible(false);
        if (serverHud)
        {
            game.ShowServerHud();
        }
        network.OpenServer();
    }
}
