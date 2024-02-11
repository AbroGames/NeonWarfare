[GameService]
public  class BattleWorldMainSceneService
{

	public BattleWorldMainSceneService()
	{
		Root.Instance.EventBus.Subscribe<BattleWorldMainSceneReadyEvent>(OnBattleWorldMainSceneReadyEvent);
	}
    
	public void OnBattleWorldMainSceneReadyEvent(BattleWorldMainSceneReadyEvent battleWorldMainSceneReadyEvent)
	{
		InitBattleWorldMainScene(battleWorldMainSceneReadyEvent.BattleWorldMainScene);
	}

	public void InitBattleWorldMainScene(BattleWorldMainScene battleWorldMainScene)
	{
		BattleHud battleHud = battleWorldMainScene.HudContainer.GetCurrentStoredNode<BattleHud>();
		BattleWorld battleWorld = battleWorldMainScene.WorldContainer.GetCurrentStoredNode<BattleWorld>();

		battleHud.BattleWorld = battleWorld;
		battleWorld.BattleHud = battleHud;
	}
}
