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
        Player player = battleWorld.Player;
        int playerRequiredXp = _playerXpService.GetRequiredXp(player);
        
        battleHud.Xp.Value = (double) player.Xp / playerRequiredXp;
        battleHud.XpLabel.Text = $"Xp: {player.Xp} / {playerRequiredXp}";
        battleHud.Level.Text = $"Level: {player.Level}";
		
        battleHud.Waves.Text = $"Wave: {battleWorld.EnemyWave.WaveNumber}";
        battleHud.Enemies.Text = $"Enemies: {battleWorld.Enemies.Count}";

        battleHud.HpBar.CurrentUpperValue = player.Hp;
        battleHud.HpBar.CurrentLowerValue = player.Hp + player.HpCanBeFastRegen;
        battleHud.HpBar.MaxValue = player.MaxHp;
        battleHud.HpBar.Label.Text = $"Health: {player.Hp:N0} / {player.MaxHp:N0}";
        battleHud.Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";

        var shader = battleHud.TimerSprite.Material as ShaderMaterial;
        shader.SetShaderParameter("Progress", 1-battleWorld.EnemyWave.NextWaveTimer / battleWorld.EnemyWave.WaveTimeout);

        battleHud.TimerLabel.Text = battleWorld.EnemyWave.NextWaveTimer.ToString("N0");
    }
}