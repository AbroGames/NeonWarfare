using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// A host without ENet: its only peer is its own.
/// </summary>
public class SingleplayerGameStarter(
    string saveFileName
    ) : BaseGameStarter, IServerOwner
{
    public override void Init(Game game)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        ResumableGame lastGame = ResumableGame.GetSingleplayer(saveFileName);
        SetLastGame(lastGame);
        ISaveFiles saveFiles = SaveFilesUpdatingLastGame(lastGame);
        LocalPlayer localPlayer = ReadLocalPlayer();
        World world = AddServerWorld(
            saveFileName,
            origin => game.AddWorld(new WorldSetup.Host(saveFiles, localPlayer, this), origin, Game.Screen.Hud),
            out string loadError);
        if (world == null)
        {
            GoToMenuAndShowError(loadError);
            return;
        }

        GoToMenuOnJoinRejected(game);
        ClearLoadingScreenOnJoined(game);
        game.SendJoinRequest(localPlayer);
    }

    // The admin is this process's own player, which leaves only with the process: there is nothing to stop
    public void AdminLeft() { }
}
