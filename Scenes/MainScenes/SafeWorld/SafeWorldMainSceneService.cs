using AbroDraft.World;
using KludgeBox;
using KludgeBox.Events;

namespace AbroDraft;

[GameService]
public  class SafeWorldMainSceneService
{
    
	[GameEventListener]
	public void OnSafeWorldMainSceneReadyEvent(SafeWorldMainSceneReadyEvent safeWorldMainSceneReadyEvent)
	{
		SafeWorldMainScene safeWorldMainScene = safeWorldMainSceneReadyEvent.SafeWorldMainScene;
		
		SafeHud safeHud = safeWorldMainScene.HudContainer.GetCurrentStoredNode<SafeHud>();
		SafeWorld safeWorld = safeWorldMainScene.WorldContainer.GetCurrentStoredNode<SafeWorld>();

		safeHud.SafeWorld = safeWorld;
		safeWorld.SafeHud = safeHud;
	}
}