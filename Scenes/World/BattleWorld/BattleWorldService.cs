using Godot;

public class BattleWorldService
{
    
    public BattleWorldService()
    {
        Root.Instance.EventBus.Subscribe<BattleWorldReadyEvent>(OnBattleWorldReadyEvent);
    }
    
    public void OnBattleWorldReadyEvent(BattleWorldReadyEvent battleWorldReadyEvent)
    {
        InitBattleWorld(battleWorldReadyEvent.BattleWorld);
    }

    public void InitBattleWorld(BattleWorld battleWorld)
    {
        
    }
}