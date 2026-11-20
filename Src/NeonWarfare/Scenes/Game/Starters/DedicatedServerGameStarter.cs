using Godot;
using GodotBox.Godot.Nodes.Process;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// A server without a player of its own.
/// </summary>
public class DedicatedServerGameStarter(
    string saveFileName,
    int? port,
    string adminUid,
    int? parentPid,
    bool serverHud,
    bool mustSetLastGame
    ) : BaseHostGameStarter(saveFileName, port, mustSetLastGame, isDedicated: true), IServerOwner
{
    private const string AdminLeftLog = "The admin of a server started by its client has left. Shutdown server.";

    private readonly ILogger _log = LogFactory.GetForStatic<DedicatedServerGameStarter>();

    public override void Init(Game game)
    {
        if (parentPid.HasValue)
        {
            ProcessDeadChecker clientDeadChecker = new ProcessDeadChecker(
                parentPid.Value, 
                () => Services.MainScene.Shutdown(),
                pid => $"Parent process {pid} is dead. Shutdown server.");
            game.AddChild(clientDeadChecker);
        }

        base.Init(game);
    }

    protected override World AddWorld(Game game, WorldOrigin origin, ISaveFiles saveFiles)
    {
        World world = game.AddWorld(
            new WorldSetup.Dedicated(saveFiles, new WorldAdmin(adminUid), this), origin,
            serverHud ? Game.Screen.ServerHud : Game.Screen.None);
        world.SetVisible(false);
        return world;
    }

    // The client that started this server has left it: the server is stopped gracefully, so it saves on exit.
    // A server started from the console outlives its admin
    public void AdminLeft()
    {
        if (!parentPid.HasValue) return;

        _log.Information(AdminLeftLog);
        Services.MainScene.Shutdown();
    }

    // A server without a port has nobody to serve
    protected override void OnHostingFailed(Error error)
    {
        Services.MainScene.Shutdown();
    }

    // Starting a new world instead would overwrite the save on exit
    protected override void OnLoadFailed(string message)
    {
        Services.MainScene.Shutdown();
    }
}
