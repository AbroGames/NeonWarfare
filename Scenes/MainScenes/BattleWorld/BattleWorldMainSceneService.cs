using AbroDraft.World;
using KludgeBox;
using KludgeBox.Events;

namespace AbroDraft;

[GameService]
public  class BattleWorldMainSceneService
{
    
	[GameEventListener]
	public void OnBattleWorldMainSceneReadyEvent(BattleWorldMainSceneReadyEvent battleWorldMainSceneReadyEvent)
	{
		BattleWorldMainScene battleWorldMainScene = battleWorldMainSceneReadyEvent.BattleWorldMainScene;
		
		BattleHud battleHud = battleWorldMainScene.HudContainer.GetCurrentStoredNode<BattleHud>();
		BattleWorld battleWorld = battleWorldMainScene.WorldContainer.GetCurrentStoredNode<BattleWorld>();

		battleHud.BattleWorld = battleWorld;
		battleWorld.BattleHud = battleHud;
	}
}