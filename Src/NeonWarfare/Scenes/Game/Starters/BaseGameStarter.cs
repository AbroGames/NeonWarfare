using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using NeonWarfare.Scripts.GlobalServices;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;
using NeonWarfare.Scripts.GlobalServices.Settings;
using Serilog;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// Brings one game session up inside a fresh <see cref="Game"/>.
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

    public abstract void Init(Game game);

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
    /// <returns>
    /// <c>null</c> when the save cannot be loaded: the error is logged, and <paramref name="errorMessage"/> is for the
    /// player.
    /// </returns>
    protected World.World AddServerWorld(
        string saveFileName, Func<WorldOrigin, World.World> addWorld, out string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(saveFileName);
        errorMessage = null;
        try
        {
            WorldOrigin origin = Services.SaveLoad.CheckFileExists(saveFileName)
                ? new WorldOrigin.FromSave(Services.SaveLoad.LoadFromDisk(saveFileName), saveFileName)
                : new WorldOrigin.NewWorld(saveFileName);
            return addWorld(origin);
        }
        catch (Exception e) when (e is SaveLoadService.LoadException or SaveFormatException)
        {
            _log.Error(e, LoadFailedLog, saveFileName);
            errorMessage = e is SaveVersionMismatchException ? VersionMismatchMessage : BrokenSaveMessage;
            return null;
        }
    }

    protected void SendJoinRequest(Game game)
    {
        GameSettings settings = Services.GameSettings.GetSettings();
        game.SendJoinRequest(settings.PlayerUid, settings.PlayerNick, settings.PlayerColor);
    }

    /// <summary>
    /// Not for a dedicated server: it has no menu.<br/>
    /// The error is logged by whoever detected it, so the server side of the failure gets logged too.
    /// </summary>
    protected void GoToMenuAndShowError(string message)
    {
        Services.MainScene.StartMainMenu(message);
        Services.LoadingScreen.Clear();
    }

    /// <summary>
    /// Not for a dedicated server: it has no menu.
    /// </summary>
    protected void GoToMenu()
    {
        Services.MainScene.StartMainMenu();
        Services.LoadingScreen.Clear();
    }
}
