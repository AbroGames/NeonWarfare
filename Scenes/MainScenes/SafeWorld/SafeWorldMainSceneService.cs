using AbroDraft.Scripts.EventBus;
using KludgeBox;
using KludgeBox.Events;

namespace AbroDraft.Scenes.MainScenes.SafeWorld;

[GameService]
public  class SafeWorldMainSceneService
{
    
	[GameEventListener]
	public void OnSafeWorldMainSceneReadyEvent(SafeWorldMainSceneReadyEvent safeWorldMainSceneReadyEvent)
	{
		SafeWorldMainScene safeWorldMainScene = safeWorldMainSceneReadyEvent.SafeWorldMainScene;
		
		Screen.SafeHud.SafeHud safeHud = safeWorldMainScene.HudContainer.GetCurrentStoredNode<Screen.SafeHud.SafeHud>();
		World.SafeWorld.SafeWorld safeWorld = safeWorldMainScene.WorldContainer.GetCurrentStoredNode<World.SafeWorld.SafeWorld>();

		safeHud.SafeWorld = safeWorld;
		safeWorld.SafeHud = safeHud;
	}
}