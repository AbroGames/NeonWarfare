using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scripts;
using NeonWarfare.Scenes.Worlds.Infra.Server.Saves;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;
using NeonWarfare.Scripts.GlobalServices.Settings;
using Serilog;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// Brings one game session up inside a fresh <see cref="Game"/>, see Docs/Startup-flow.md#level-2-gamestarter.
/// The helpers are the steps several modes share.
/// </summary>
public abstract class BaseGameStarter
{
    protected const string Localhost = Consts.Localhost;
    protected const string DefaultHost = Consts.DefaultHost;
    protected const int DefaultPort = Consts.DefaultPort;

    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md.
    private const string VersionMismatchMessage = "The save was made by another version of the game";
    private const string BrokenSaveMessage = "The save is broken or cannot be read";
    private const string WorldFailedMessage = "Failed to create the world";

    private readonly ILogger _log = LogFactory.GetForStatic<BaseGameStarter>();

    public abstract void Start(Game game);

    protected void SetLastGame(ResumableGame lastGame)
    {
        Services.LastGame.SetLastGame(lastGame);
    }

    /// <summary>
    /// The save files of a World that "Continue" leads to. "Continue" follows the file of the last save.
    /// </summary>
    protected ISaveFiles SaveFilesUpdatingLastGame(ResumableGame lastGame) =>
        new LastGameUpdatingSaveFiles(
            Services.SaveLoad, saveFileName => SetLastGame(lastGame with { SaveName = saveFileName }));

    /// <summary>
    /// Loads the server World from its save file if there is one. Otherwise it is a new World.
    /// </summary>
    /// <exception cref="SaveLoadService.LoadException">The file cannot be read.</exception>
    protected WorldOrigin LoadServerOrigin(string saveFileName)
    {
        ArgumentNullException.ThrowIfNull(saveFileName);
        return Services.SaveLoad.CheckFileExists(saveFileName)
            ? new WorldOrigin.FromSave(Services.SaveLoad.LoadFromDisk(saveFileName))
            : new WorldOrigin.NewWorld();
    }

    /// <summary>
    /// Logs any exception of building the server World and turns it into a message for the player.
    /// </summary>
    /// <returns>The message for the player.</returns>
    protected string LogWorldError(Exception e, string saveFileName)
    {
        _log.Error(e, "Creating the World of the save '{saveFileName}' failed", saveFileName);
        return e switch
        {
            SaveVersionMismatchException => VersionMismatchMessage,
            SaveLoadService.LoadException or SaveFormatException => BrokenSaveMessage,
            _ => WorldFailedMessage
        };
    }

    /// <summary>
    /// Reads the player once per session.
    /// The same object goes to the World and into the join request, so a settings change in between cannot make them
    /// disagree.
    /// </summary>
    protected LocalPlayer ReadLocalPlayer()
    {
        GameSettings settings = Services.GameSettings.GetSettings();
        return new LocalPlayer(new PlayerUid(settings.PlayerUid), settings.PlayerNick, settings.PlayerColor);
    }

    /// <summary>
    /// A failed session of this process's player goes to the menu with its message.
    /// On the join the loading screen gives way to the <c>Hud</c>.
    /// </summary>
    protected void FollowLocalPlayer(Game game)
    {
        void OnFailed(string message) => GoToMenuAndShowError(message);
        void OnLocalPlayerJoined() => Services.LoadingScreen.Clear();

        // The handlers die with the Game that raises the events, so nothing unsubscribes.
        game.Failed += OnFailed;
        game.LocalPlayerJoined += OnLocalPlayerJoined;
    }

    /// <summary>
    /// Connects a remote client. The connecting screen stays until the join, and its cancel leads back to the menu.
    /// </summary>
    protected void ConnectAndFollow(Game game, LocalPlayer localPlayer, string host, int port)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Connecting, GoToMenu);
        FollowLocalPlayer(game);
        game.ConnectToServer(localPlayer, host, port);
    }

    /// <summary>
    /// Not for a dedicated server, which has no menu.
    /// The error is logged by whoever detected it, so the server side of the failure gets logged too.
    /// </summary>
    protected void GoToMenuAndShowError(string message)
    {
        Services.MainScene.StartMainMenu(message);
    }

    /// <summary>Not for a dedicated server, which has no menu.</summary>
    protected void GoToMenu()
    {
        Services.MainScene.StartMainMenu();
    }
}
