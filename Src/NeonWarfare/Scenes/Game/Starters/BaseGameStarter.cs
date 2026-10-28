using NeonWarfare.Scripts.GlobalServices.ResumableGame;
using NeonWarfare.Scripts.GlobalServices.Settings;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// Brings one game session up inside a fresh <see cref="Game"/>.
/// </summary>
public abstract class BaseGameStarter
{
    protected const string Localhost = Consts.Localhost;
    protected const string DefaultHost = Consts.DefaultHost;
    protected const int DefaultPort = Consts.DefaultPort;

    public abstract void Init(Game game);

    protected void SetLastGame(ResumableGame lastGame)
    {
        Services.LastGame.SetLastGame(lastGame);
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
