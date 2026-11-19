using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// A host without ENet: its only peer is its own.
/// </summary>
public class SingleplayerGameStarter(
    string saveFileName
    ) : BaseGameStarter
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
            origin => game.AddWorld(
                WorldLayer.Host, origin, Game.Screen.Hud, saveFiles, localPlayer, new WorldAdmin(localPlayer.Uid)),
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
}
