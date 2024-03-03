using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global.World;
using SafeWorld = NeoVector.SafeWorld;

namespace KludgeBox.Events.Global;

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