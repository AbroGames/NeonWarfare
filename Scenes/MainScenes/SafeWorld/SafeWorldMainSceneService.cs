[GameService]
public  class SafeWorldMainSceneService
{

	public SafeWorldMainSceneService()
	{
		Root.Instance.EventBus.Subscribe<SafeWorldMainSceneReadyEvent>(OnSafeWorldMainSceneReadyEvent);
	}
    
	public void OnSafeWorldMainSceneReadyEvent(SafeWorldMainSceneReadyEvent safeWorldMainSceneReadyEvent)
	{
		InitSafeWorldMainScene(safeWorldMainSceneReadyEvent.SafeWorldMainScene);
	}

	public void InitSafeWorldMainScene(SafeWorldMainScene safeWorldMainScene)
	{
		SafeHud safeHud = safeWorldMainScene.HudContainer.GetCurrentStoredNode<SafeHud>();
		SafeWorld safeWorld = safeWorldMainScene.WorldContainer.GetCurrentStoredNode<SafeWorld>();

		safeHud.SafeWorld = safeWorld;
		safeWorld.SafeHud = safeHud;
	}
}
