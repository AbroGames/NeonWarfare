using System;
using Godot;
using Humanizer;
using NeonWarfare.Scenes.Game.Networks;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>A server hosted from inside the client. The host plays in it too.</summary>
public class HostMultiplayerGameStarter(string saveFileName, int? port) : BaseGameStarter
{
    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md.
    private const string HostingFailedMessage = "Failed to start server: {0}";

    public override void Start(Game game)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        Network network = game.AddServerNetwork();
        ResumableGame lastGame = ResumableGame.GetCreateServer(saveFileName, port ?? DefaultPort, isDedicated: false);
        SetLastGame(lastGame);

        Error error = network.HostServer(port ?? DefaultPort);
        if (error != Error.Ok)
        {
            // Network has already logged the error.
            GoToMenuAndShowError(HostingFailedMessage.FormatWith(error));
            return;
        }

        FollowLocalPlayer(game);
        try
        {
            // The host's join leaves before the server is opened, so the host is the first to join.
            game.AddHostWorld(
                network, SaveFilesUpdatingLastGame(lastGame), ReadLocalPlayer(), LoadServerOrigin(saveFileName));
        }
        catch (Exception e)
        {
            // The server is not opened yet, so nobody has connected.
            GoToMenuAndShowError(LogWorldError(e, saveFileName));
            return;
        }

        network.OpenServer();
    }
}
