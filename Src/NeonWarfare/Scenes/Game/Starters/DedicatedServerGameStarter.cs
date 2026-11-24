using Godot;
using GodotBox.Godot.Nodes.Process;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Infra.Composition;

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
    ) : BaseHostGameStarter(saveFileName, port, adminUid, mustSetLastGame, isDedicated: true)
{
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

    protected override World.World AddWorld(Game game, WorldOrigin origin)
    {
        World.World world = game.AddWorld(
            WorldLayer.Dedicated, origin, serverHud ? Game.Screen.ServerHud : Game.Screen.None);
        world.SetVisible(false);
        return world;
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
