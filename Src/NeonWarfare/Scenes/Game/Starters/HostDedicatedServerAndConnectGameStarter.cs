using GodotBox.Godot.Nodes.Process;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// Launches a dedicated server as a child process and connects to it as an ordinary client.
/// </summary>
public class HostDedicatedServerAndConnectGameStarter(
    string saveFileName,
    int? port,
    bool showWindow
    ) : ConnectToMultiplayerGameStarter(host: Localhost, port: port, mustSetLastGame: false)
{
    private readonly int? _port = port;

    public override void Init(Game game)
    {
        // The admin of the child server is the very player that joins it
        LocalPlayer localPlayer = ReadLocalPlayer();
        int dedicatedServerPid = Services.Process.StartNewDedicatedServerApplication(
            saveFileName,
            _port ?? DefaultPort,
            localPlayer.Uid,
            showWindow);
        
        ProcessShutdowner dedicatedServerShutdowner = new ProcessShutdowner(
            dedicatedServerPid,
            pid => $"Kill server process: {pid}."); 
        game.AddChild(dedicatedServerShutdowner);

        // Try to connect to new hosted server, don't save connect as last game
        // Flag 'SetLastGame = false' was set in constructor
        Connect(game, localPlayer);
        
        // This starter always start from menu, so we must set LastGame  
        var lastGame = ResumableGame.GetCreateServer(saveFileName, _port ?? DefaultPort, true);
        SetLastGame(lastGame);
    }
}