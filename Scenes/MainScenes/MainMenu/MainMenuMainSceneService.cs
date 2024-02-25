using AbroDraft.Scripts.EventBus;

namespace AbroDraft.Scenes.MainScenes.MainMenu;

public class MainMenuMainSceneService
{

	public MainMenuMainSceneService()
	{
		EventBus.Subscribe<MainMenuMainSceneReadyEvent>(OnMainMenuMainSceneReadyEvent);
	}
    
	public void OnMainMenuMainSceneReadyEvent(MainMenuMainSceneReadyEvent mainMenuMainSceneReadyEvent)
	{
		InitMainMenuMainScene(mainMenuMainSceneReadyEvent.MainMenuMainScene);
	}

	public void InitMainMenuMainScene(MainMenuMainScene mainMenuMainScene)
	{
		
	}
}