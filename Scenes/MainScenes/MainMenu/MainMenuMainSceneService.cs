public  class MainMenuMainSceneService
{

	public MainMenuMainSceneService()
	{
		Root.Instance.EventBus.Subscribe<MainMenuMainSceneReadyEvent>(OnMainMenuMainSceneReadyEvent);
	}
    
	public void OnMainMenuMainSceneReadyEvent(MainMenuMainSceneReadyEvent mainMenuMainSceneReadyEvent)
	{
		InitMainMenuMainScene(mainMenuMainSceneReadyEvent.MainMenuMainScene);
	}

	public void InitMainMenuMainScene(MainMenuMainScene mainMenuMainScene)
	{
		
	}
}
