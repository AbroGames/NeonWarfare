using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global.World;

namespace KludgeBox.Events.Global;

[GameService]
public  class BattleWorldMainSceneService
{
    
	[EventListener]
	public void OnBattleWorldMainSceneReadyEvent(BattleWorldMainSceneReadyEvent battleWorldMainSceneReadyEvent)
	{
		BattleWorldMainScene battleWorldMainScene = battleWorldMainSceneReadyEvent.BattleWorldMainScene;
		
		BattleHud battleHud = battleWorldMainScene.HudContainer.GetCurrentStoredNode<BattleHud>();
		BattleWorld battleWorld = battleWorldMainScene.WorldContainer.GetCurrentStoredNode<BattleWorld>();

		battleHud.BattleWorld = battleWorld;
		battleWorld.BattleHud = battleHud;
	}
}