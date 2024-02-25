using AbroDraft.Scripts.EventBus;
using AbroDraft.Scripts.Utils;

namespace AbroDraft.Scenes.MainScenes.SafeWorld;

[GameService]
public  class SafeWorldMainSceneService
{

	public SafeWorldMainSceneService()
	{
		EventBus.Subscribe<SafeWorldMainSceneReadyEvent>(OnSafeWorldMainSceneReadyEvent);
	}
    
	public void OnSafeWorldMainSceneReadyEvent(SafeWorldMainSceneReadyEvent safeWorldMainSceneReadyEvent)
	{
		InitSafeWorldMainScene(safeWorldMainSceneReadyEvent.SafeWorldMainScene);
	}

	public void InitSafeWorldMainScene(SafeWorldMainScene safeWorldMainScene)
	{
		Screen.SafeHud.SafeHud safeHud = safeWorldMainScene.HudContainer.GetCurrentStoredNode<Screen.SafeHud.SafeHud>();
		World.SafeWorld.SafeWorld safeWorld = safeWorldMainScene.WorldContainer.GetCurrentStoredNode<World.SafeWorld.SafeWorld>();

		safeHud.SafeWorld = safeWorld;
		safeWorld.SafeHud = safeHud;
	}
}