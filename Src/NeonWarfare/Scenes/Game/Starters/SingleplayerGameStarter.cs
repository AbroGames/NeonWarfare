using NeonWarfare.Scenes.World.Infra.Composition;
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
        World.World world = AddServerWorld(
            saveFileName, origin => game.AddWorld(WorldLayer.Host, origin, Game.Screen.Hud), out string loadError);
        if (world == null)
        {
            GoToMenuAndShowError(loadError);
            return;
        }
        UpdateLastGameOnSave(world, lastGame);

        SendJoinRequest(game);
        Services.LoadingScreen.Clear();
    }
}
