using System;
using Godot;
using Humanizer;
using NeonWarfare.Scenes.Game.Networks;
using NeonWarfare.Scenes.Worlds.Features.Options;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>A server hosted from inside the client. The host plays in it too.</summary>
/// <param name="newWorldOptions">Used only when there is no save file yet.</param>
public class HostMultiplayerGameStarter(SaveFileName saveFileName, int? port, WorldOptions newWorldOptions)
    : BaseGameStarter
{
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
            GoToMenuAndShowError(Services.I18N.Tr("MESSAGE_MENU__HOSTING_FAILED").FormatWith(error));
            return;
        }

        FollowLocalPlayer(game);
        try
        {
            // The host's join leaves before the server is opened, so the host is the first to join.
            game.AddHostWorld(network, SaveFilesUpdatingLastGame(lastGame),
                new WorldSaving(saveFileName, Services.GameSettings.GetSettings().AutoSaveIntervalMinutes),
                ReadLocalPlayer(), LoadServerOrigin(saveFileName, () => newWorldOptions));
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
