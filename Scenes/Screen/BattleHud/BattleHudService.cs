using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

public class BattleHudService
{

    private readonly PlayerXpService _playerXpService;
    private readonly Stopwatch _physicsStopwatch = new();
    private readonly Queue<double> _deltas = new();
    
    public BattleHudService(PlayerXpService playerXpService)
    {
        _playerXpService = playerXpService;
        
        Root.Instance.EventBus.Subscribe<BattleHudProcessEvent>(OnBattleHudProcessEvent);
        Root.Instance.EventBus.Subscribe<BattleHudPhysicsProcessEvent>(OnBattleHudPhysicsProcessEvent);
    }
    
    public void OnBattleHudProcessEvent(BattleHudProcessEvent battleHudProcessEvent) 
    {
        UpdateBattleHud(battleHudProcessEvent.BattleHud, battleHudProcessEvent.BattleHud.BattleWorld);
    }
    
    public void OnBattleHudPhysicsProcessEvent(BattleHudPhysicsProcessEvent physEvent)
    {
        UpdateHudPhysics(physEvent.BattleHud, physEvent.BattleHud.BattleWorld);
    }

    /// <summary>
    /// Мы используем настолько альтернативный подход к расчету ТПС потому, что все значения физической дельты в движке - константы.
    /// Даже если реальный ТПС упадёт до 1, дельта, приходящая в _PhysicsProcess будет 1/60.
    /// </summary>
    /// <param name="battleHud"></param>
    /// <param name="battleWorld"></param>
    public void UpdateHudPhysics(BattleHud battleHud, BattleWorld battleWorld)
    {
        var delta = _physicsStopwatch.Elapsed.TotalSeconds;
        _deltas.Enqueue(delta);
        if (_deltas.Count >= 120)
        {
            var tps = _deltas.Average();
            battleHud.Tps.Text = $"TPS: {1/tps:N0}";
            _deltas.Dequeue();
        }
        _physicsStopwatch.Restart();
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

        battleHud.BasicAbilityIcon.Progress = player.BasicAbilityCd.FractionElapsed;
                    

        var shader = battleHud.TimerSprite.Material as ShaderMaterial;
        shader.SetShaderParameter("Progress", 1-battleWorld.EnemyWave.NextWaveTimer / battleWorld.EnemyWave.WaveTimeout);

        battleHud.TimerLabel.Text = battleWorld.EnemyWave.NextWaveTimer.ToString("N0");
    }

    
}