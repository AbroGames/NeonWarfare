using Godot;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>Launches a dedicated server as a child process and connects to it as an ordinary client.</summary>
public class HostDedicatedServerAndConnectGameStarter(
    string saveFileName,
    int? port,
    bool showWindow
    ) : BaseGameStarter
{
    public override void Start(Game game)
    {
        // The admin of the child server is the very player that joins it.
        LocalPlayer localPlayer = ReadLocalPlayer();
        // The child server stops when its admin leaves, see DedicatedServerGameStarter. MainScene waits for its exit.
        Error error = Services.Process.StartNewDedicatedServerApplication(
            saveFileName,
            port ?? DefaultPort,
            localPlayer.Uid,
            showWindow);
        if (error != Error.Ok)
        {
            // ProcessService has already logged the error.
            GoToMenuAndShowError(Services.I18N.Tr("MESSAGE_MENU__HOSTING_FAILED").FormatWith(error));
            return;
        }

        // "Continue" brings the server up again instead of connecting to it.
        SetLastGame(ResumableGame.GetCreateServer(saveFileName, port ?? DefaultPort, isDedicated: true));
        ConnectAndFollow(game, localPlayer, Localhost, port ?? DefaultPort);
    }
}
