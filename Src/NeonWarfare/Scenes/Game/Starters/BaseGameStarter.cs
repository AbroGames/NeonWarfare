using System;
using Godot;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Saves;
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
    protected World AddServerWorld(
        string saveFileName, Func<WorldOrigin, World> addWorld, out string errorMessage)
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

    /// <summary>
    /// Read once per session: the same object goes to the World and into the join request, so a settings change
    /// in between cannot make them disagree.
    /// </summary>
    protected LocalPlayer ReadLocalPlayer()
    {
        GameSettings settings = Services.GameSettings.GetSettings();
        return new LocalPlayer(settings.PlayerUid, settings.PlayerNick, settings.PlayerColor);
    }

    protected static bool IsGameAlive(Game game)
    {
        return GodotObject.IsInstanceValid(game) && !game.IsQueuedForDeletion();
    }

    /// <summary>
    /// For every process with a player of its own: a refused join leaves a remote client on the connecting screen
    /// and a host in a World without its player. <see cref="Game"/> dies with the handler.
    /// </summary>
    protected void GoToMenuOnJoinRejected(Game game)
    {
        // Game has already logged the reason
        game.JoinRejectedEvent += reason =>
        {
            if (IsGameAlive(game)) GoToMenuAndShowError(JoinRejectedMessage(reason));
        };
    }

    /// <summary>
    /// For every process with a player of its own: the <see cref="Game.Screen.Hud"/> appears only with the join,
    /// and until then the loading screen covers the World.
    /// </summary>
    protected void ClearLoadingScreenOnJoined(Game game)
    {
        game.LocalPlayerJoinedEvent += () =>
        {
            if (IsGameAlive(game)) Services.LoadingScreen.Clear();
        };
    }

    private static string JoinRejectedMessage(JoinRejectReason reason) => Services.I18N.Tr(reason switch
    {
        JoinRejectReason.ProtocolMismatch => "MESSAGE_MENU__JOIN_REJECTED_PROTOCOL_MISMATCH",
        JoinRejectReason.InvalidUid => "MESSAGE_MENU__JOIN_REJECTED_INVALID_UID",
        JoinRejectReason.InvalidNick => "MESSAGE_MENU__JOIN_REJECTED_INVALID_NICK",
        JoinRejectReason.InvalidColor => "MESSAGE_MENU__JOIN_REJECTED_INVALID_COLOR",
        JoinRejectReason.UidInUse => "MESSAGE_MENU__JOIN_REJECTED_UID_IN_USE",
        JoinRejectReason.InternalError => "MESSAGE_MENU__JOIN_REJECTED_INTERNAL_ERROR",
        // A server of another build may send a code this one does not know
        _ => "MESSAGE_MENU__JOIN_REJECTED_UNKNOWN"
    });

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
