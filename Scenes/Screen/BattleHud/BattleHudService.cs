using Godot;

public class BattleHudService
{
    
    public BattleHudService()
    {
        Root.Instance.EventBus.Subscribe<BattleHudReadyEvent>(OnBattleHudReadyEvent);
        Root.Instance.EventBus.Subscribe<BattleHudProcessEvent>(OnBattleHudProcessEvent);
    }
    
    public void OnBattleHudReadyEvent(BattleHudReadyEvent BattleHudReadyEvent)
    {
        InitBattleHud(BattleHudReadyEvent.BattleHud);
    }
    
    public void OnBattleHudProcessEvent(BattleHudProcessEvent BattleHudProcessEvent) 
    {
        //moveCamera(BattleHudProcessEvent.BattleHud, BattleHudProcessEvent.Delta);
    }

    public void InitBattleHud(BattleHud BattleHud)
    {
        
    }
    
}