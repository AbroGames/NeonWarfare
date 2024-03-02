using KludgeBox;
using KludgeBox.Events;

namespace KludgeBox.Events.Global.World;

[GameService]
public class PlayerDamageService
{
    [EventListener]
    public void OnPlayerDeath(PlayerDeathEvent e)
    {
        var player = e.Player;
        
        var mainMenu = Root.Instance.PackedScenes.Main.MainMenu;
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(mainMenu.Instantiate());
        EventBus.Publish(new GameResetEvent());
        Audio2D.StopMusic();
    }
}