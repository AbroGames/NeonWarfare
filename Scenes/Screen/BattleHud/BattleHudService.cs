using System.Linq;
using Godot;

public class BattleHudService
{

    private readonly PlayerXpService _playerXpService;
    
    public BattleHudService(PlayerXpService playerXpService)
    {
        _playerXpService = playerXpService;
        
        Root.Instance.EventBus.Subscribe<BattleHudProcessEvent>(OnBattleHudProcessEvent);
    }
    
    public void OnBattleHudProcessEvent(BattleHudProcessEvent battleHudProcessEvent) 
    {
        UpdateBattleHud(battleHudProcessEvent.BattleHud, battleHudProcessEvent.BattleHud.BattleWorld);
    }

    public void UpdateBattleHud(BattleHud battleHud, BattleWorld battleWorld)
    {
        int playerRequiredXp = _playerXpService.GetRequiredXp(battleWorld.Player);
        
        battleHud.Xp.Value = (double) battleWorld.Player.Xp / playerRequiredXp;
        battleHud.XpLabel.Text = $"Xp: {battleWorld.Player.Xp} / {playerRequiredXp}";
        battleHud.Level.Text = $"Level: {battleWorld.Player.Level}";
		
        battleHud.Waves.Text = $"Wave: {battleWorld.EnemyWave.WaveNumber}";
        battleHud.Enemies.Text = $"Enemies: {battleWorld.Enemies.Count}";

        battleHud.HpBar.CurrentUpperValue = battleWorld.Player.Hp;
        battleHud.HpBar.CurrentLowerValue = battleWorld.Player.Hp + battleWorld.Player.HpCanBeFastRegen;
        battleHud.HpBar.MaxValue = battleWorld.Player.MaxHp;
        battleHud.HpBar.Label.Text = $"Health: {battleWorld.Player.Hp:N0} / {battleWorld.Player.MaxHp:N0}";
        battleHud.Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";
    }
}