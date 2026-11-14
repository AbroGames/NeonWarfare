using NeonWarfare.Scenes.World;
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

        //TODO 022 load the save
        game.AddWorld(WorldLayer.Host, WorldOrigin.New, GameScreen.Hud);
        SetLastGame(ResumableGame.GetSingleplayer(saveFileName));
        SendJoinRequest(game);
        Services.LoadingScreen.Clear();
    }
}
