using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Server.Saves;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;
using NeonWarfare.Scripts.GlobalServices.Settings;
using Serilog;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// Brings one game session up inside a fresh <see cref="Game"/>, step by step; the steps several modes share are the
/// helpers below.
/// </summary>
public abstract class BaseGameStarter
{
    protected const string Localhost = Consts.Localhost;
    protected const string DefaultHost = Consts.DefaultHost;
    protected const int DefaultPort = Consts.DefaultPort;

    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md
    private const string VersionMismatchMessage = "The save was made by another version of the game";
    private const string BrokenSaveMessage = "The save is broken or cannot be read";
    private const string LoadFailedLog = "Loading the save '{saveFileName}' failed";

    private readonly ILogger _log = LogFactory.GetForStatic<BaseGameStarter>();

    public abstract void Start(Game game);

    protected void SetLastGame(ResumableGame lastGame)
    {
        Services.LastGame.SetLastGame(lastGame);
    }

    /// <summary>
    /// The save files of a World that "Continue" leads to: it follows the file of the last save.
    /// </summary>
    protected ISaveFiles SaveFilesUpdatingLastGame(ResumableGame lastGame) =>
        new LastGameUpdatingSaveFiles(
            Services.SaveLoad, saveFileName => SetLastGame(lastGame with { SaveName = saveFileName }));

    /// <summary>
    /// The server World from its save file if there is one, otherwise a new World that saves to it.
    /// </summary>
    /// <exception cref="SaveLoadService.LoadException">The file cannot be read, see <see cref="IsLoadError"/>.
    /// </exception>
    protected WorldOrigin LoadServerOrigin(string saveFileName)
    {
        ArgumentNullException.ThrowIfNull(saveFileName);
        return Services.SaveLoad.CheckFileExists(saveFileName)
            ? new WorldOrigin.FromSave(Services.SaveLoad.LoadFromDisk(saveFileName), saveFileName)
            : new WorldOrigin.NewWorld(saveFileName);
    }

    /// <summary>
    /// The file of <see cref="LoadServerOrigin"/> cannot be read, or the World cannot be built from it.
    /// </summary>
    protected static bool IsLoadError(Exception e) => e is SaveLoadService.LoadException or SaveFormatException;

    /// <returns>The message for the player.</returns>
    protected string LogLoadError(Exception e, string saveFileName)
    {
        _log.Error(e, LoadFailedLog, saveFileName);
        return e is SaveVersionMismatchException ? VersionMismatchMessage : BrokenSaveMessage;
    }

    /// <summary>
    /// Read once per session: the same object goes to the World and into the join request, so a settings change
    /// in between cannot make them disagree.
    /// </summary>
    protected LocalPlayer ReadLocalPlayer()
    {
        GameSettings settings = Services.GameSettings.GetSettings();
        return new LocalPlayer(settings.PlayerUid, settings.PlayerNick, settings.PlayerColor);
    }

    /// <summary>
    /// A session of this process's player ends in the menu with its message, and with the join the loading screen
    /// gives way to the <c>Hud</c>.
    /// </summary>
    protected void FollowLocalPlayer(Game game)
    {
        void OnFailed(string message) => GoToMenuAndShowError(message);
        void OnLocalPlayerJoined() => Services.LoadingScreen.Clear();

        // Events of the Game itself, which holds the handlers and dies first, so nothing unsubscribes
        game.Failed += OnFailed;
        game.LocalPlayerJoined += OnLocalPlayerJoined;
    }

    /// <summary>
    /// A remote client: until the join, the connecting screen, whose cancel leads back to the menu.
    /// </summary>
    protected void ConnectAndFollow(Game game, LocalPlayer localPlayer, string host, int port)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Connecting, GoToMenu);
        FollowLocalPlayer(game);
        game.ConnectToServer(localPlayer, host, port);
    }

    /// <summary>
    /// Not for a dedicated server: it has no menu.<br/>
    /// The error is logged by whoever detected it, so the server side of the failure gets logged too.
    /// </summary>
    protected void GoToMenuAndShowError(string message)
    {
        Services.MainScene.StartMainMenu(message);
    }

    /// <summary>
    /// Not for a dedicated server: it has no menu.
    /// </summary>
    protected void GoToMenu()
    {
        Services.MainScene.StartMainMenu();
    }
}
