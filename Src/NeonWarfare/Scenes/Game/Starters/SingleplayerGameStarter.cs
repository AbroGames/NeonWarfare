using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;
using NeonWarfare.Scripts.GlobalServices.Settings;

namespace NeonWarfare.Scenes.Game.Starters;

public class SingleplayerGameStarter(
    string saveFileName
    ) : BaseGameStarter
{
    
    public override void Init(Game game)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        GameSettings gameSettings = Services.GameSettings.GetSettings();
        OldWorld.World world = game.AddWorld();
        game.AddHud();
        
        var lastGame = ResumableGame.GetSingleplayer(saveFileName);
        SetLastGame(lastGame);
        AddLastGameUpdaterToSaveEvent(world, lastGame);
        
        ServerStartWorld(world, saveFileName, gameSettings.PlayerUid);
        ClientStartWorld(world);
    }
}