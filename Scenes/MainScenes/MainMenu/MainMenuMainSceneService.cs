using AbroDraft.Scripts.EventBus;
using KludgeBox.Events;

namespace AbroDraft.Scenes.MainScenes.MainMenu;

public class MainMenuMainSceneService
{
    
	[GameEventListener]
	public void OnMainMenuMainSceneReadyEvent(MainMenuMainSceneReadyEvent mainMenuMainSceneReadyEvent) { }
}