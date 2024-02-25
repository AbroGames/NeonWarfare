using AbroDraft.Scripts.EventBus;
using KludgeBox;
using KludgeBox.Events;

namespace AbroDraft.Scenes.MainScenes.BattleWorld;

[GameService]
public  class BattleWorldMainSceneService
{
    
	[GameEventListener]
	public void OnBattleWorldMainSceneReadyEvent(BattleWorldMainSceneReadyEvent battleWorldMainSceneReadyEvent)
	{
		BattleWorldMainScene battleWorldMainScene = battleWorldMainSceneReadyEvent.BattleWorldMainScene;
		
		Screen.BattleHud.BattleHud battleHud = battleWorldMainScene.HudContainer.GetCurrentStoredNode<Screen.BattleHud.BattleHud>();
		World.BattleWorld.BattleWorld battleWorld = battleWorldMainScene.WorldContainer.GetCurrentStoredNode<World.BattleWorld.BattleWorld>();

		battleHud.BattleWorld = battleWorld;
		battleWorld.BattleHud = battleHud;
	}
}