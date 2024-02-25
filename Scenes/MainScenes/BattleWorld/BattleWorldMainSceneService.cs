using AbroDraft.Scripts.EventBus;
using AbroDraft.Scripts.Utils;

namespace AbroDraft.Scenes.MainScenes.BattleWorld;

[GameService]
public  class BattleWorldMainSceneService
{

	public BattleWorldMainSceneService()
	{
		EventBus.Subscribe<BattleWorldMainSceneReadyEvent>(OnBattleWorldMainSceneReadyEvent);
	}
    
	public void OnBattleWorldMainSceneReadyEvent(BattleWorldMainSceneReadyEvent battleWorldMainSceneReadyEvent)
	{
		InitBattleWorldMainScene(battleWorldMainSceneReadyEvent.BattleWorldMainScene);
	}

	public void InitBattleWorldMainScene(BattleWorldMainScene battleWorldMainScene)
	{
		Screen.BattleHud.BattleHud battleHud = battleWorldMainScene.HudContainer.GetCurrentStoredNode<Screen.BattleHud.BattleHud>();
		World.BattleWorld.BattleWorld battleWorld = battleWorldMainScene.WorldContainer.GetCurrentStoredNode<World.BattleWorld.BattleWorld>();

		battleHud.BattleWorld = battleWorld;
		battleWorld.BattleHud = battleHud;
	}
}