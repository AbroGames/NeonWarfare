using System;
using Godot;
using Humanizer;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// A server hosted from inside the client: the host plays in it too.
/// </summary>
public class HostMultiplayerGameStarter(string saveFileName, int? port) : BaseGameStarter
{
    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md
    private const string HostingFailedMessage = "Failed to start server: {0}";

    public override void Start(Game game)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        Network network = game.AddServerNetwork();
        ResumableGame lastGame = ResumableGame.GetCreateServer(saveFileName, port ?? DefaultPort, isDedicated: false);
        SetLastGame(lastGame);

        // Network has already logged the error
        Error error = network.HostServer(port ?? DefaultPort);
        if (error != Error.Ok)
        {
            GoToMenuAndShowError(HostingFailedMessage.FormatWith(error));
            return;
        }

        FollowLocalPlayer(game);
        try
        {
            // The host's join leaves before the server is opened, so the host is the first to join
            game.AddHostWorld(SaveFilesUpdatingLastGame(lastGame), ReadLocalPlayer(), LoadServerOrigin(saveFileName));
        }
        catch (Exception e) when (IsLoadError(e))
        {
            // The server is not opened: nobody has connected yet
            GoToMenuAndShowError(LogLoadError(e, saveFileName));
            return;
        }

        network.OpenServer();
    }
}
