using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeoVector;

[GameService]
public class PlayerDamageService
{
    [EventListener]
    public void OnPlayerDeath(PlayerDeathEvent e)
    {
        var player = e.Player;
        
        var mainMenu = Root.Instance.PackedScenes.Main.MainMenu;
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(mainMenu.Instantiate());
        Audio2D.StopMusic();
    }
}