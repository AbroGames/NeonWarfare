using KludgeBox;
using KludgeBox.Events;

namespace NeoVector;

[GameService]
public  class SafeWorldMainSceneService
{
    
	[EventListener]
	public void OnSafeWorldMainSceneReadyEvent(SafeWorldMainSceneReadyEvent safeWorldMainSceneReadyEvent)
	{
		SafeWorldMainScene safeWorldMainScene = safeWorldMainSceneReadyEvent.SafeWorldMainScene;
		
		SafeHud safeHud = safeWorldMainScene.HudContainer.GetCurrentStoredNode<SafeHud>();
		SafeWorld safeWorld = safeWorldMainScene.WorldContainer.GetCurrentStoredNode<SafeWorld>();

		safeHud.SafeWorld = safeWorld;
		safeWorld.SafeHud = safeHud;
	}
}