using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.Service.Settings;

namespace NeonWarfare.Scenes.Game.Starters;

public class SingleplayerGameStarter(string saveFileName = null) : BaseGameStarter
{
    
    public override void Init(Game game)
    {
        base.Init(game);
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        GameSettings gameSettings = Services.GameSettings.GetSettings();
        World.World world = game.AddWorld();
        game.AddHud();
        
        ServerStartWorld(world, saveFileName, gameSettings.PlayerNick);
        ClientStartWorld(world);
    }
}