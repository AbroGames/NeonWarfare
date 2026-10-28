using GodotBox.Godot.Nodes.Process;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// Launches a dedicated server as a child process and connects to it as an ordinary client.
/// </summary>
public class HostDedicatedServerAndConnectGameStarter(
    string saveFileName,
    int? port,
    string adminUid,
    bool showWindow
    ) : ConnectToMultiplayerGameStarter(host: Localhost, port: port, mustSetLastGame: false)
{
    private readonly int? _port = port;

    public override void Init(Game game)
    {
        int dedicatedServerPid = Services.Process.StartNewDedicatedServerApplication(
            saveFileName,
            _port ?? DefaultPort,
            adminUid,
            showWindow);
        
        ProcessShutdowner dedicatedServerShutdowner = new ProcessShutdowner(
            dedicatedServerPid,
            pid => $"Kill server process: {pid}."); 
        game.AddChild(dedicatedServerShutdowner);

        // Try to connect to new hosted server, don't save connect as last game
        // Flag 'SetLastGame = false' was set in constructor
        base.Init(game); 
        
        // This starter always start from menu, so we must set LastGame  
        var lastGame = ResumableGame.GetCreateServer(saveFileName, _port ?? DefaultPort, true);
        SetLastGame(lastGame);
    }
}