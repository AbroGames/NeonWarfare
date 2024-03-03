using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeoVector;

[GameService]
public class BattleHudService
{

    private readonly Stopwatch _physicsStopwatch = new();
    private readonly Queue<double> _deltas = new();
    
    [EventListener]
    public void OnBattleHudProcessEvent(BattleHudProcessEvent battleHudProcessEvent)
    {
        double delta = battleHudProcessEvent.Delta;
        BattleHud battleHud = battleHudProcessEvent.BattleHud;
        BattleWorld battleWorld = battleHud.BattleWorld;
        Player player = battleWorld.Player;
        
        battleHud.Xp.Value = (double) player.Xp / player.NextLevelXp;
        battleHud.XpLabel.Text = $"Xp: {player.Xp} / {player.NextLevelXp}";
        battleHud.Level.Text = $"Level: {player.Level}";
		
        battleHud.Waves.Text = $"Wave: {battleWorld.EnemyWave.WaveNumber}";
        battleHud.Enemies.Text = $"Enemies: {battleWorld.Enemies.Count}";
        
        battleHud.HpBar.CurrentUpperValue = player.Hp;
        double hpBarValueDelta = Mathf.Clamp(battleHud.HpBar.CurrentLowerValue - battleHud.HpBar.CurrentUpperValue, 
            0, Math.Max(battleHud.HpBar.MaxValue - battleHud.HpBar.CurrentUpperValue, 0));
        double hpBarValueDeltaDecrease = battleHud.HpBar.MaxValue * 0.25 * delta;
        battleHud.HpBar.CurrentLowerValue = player.Hp + hpBarValueDelta - hpBarValueDeltaDecrease;
        
        battleHud.HpBar.MaxValue = player.MaxHp;
        battleHud.HpBar.Label.Text = $"Health: {player.Hp:N0} / {player.MaxHp:N0}";
        battleHud.Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";

        battleHud.BeamIcon.Progress = player.BasicAbilityCd.FractionElapsed;
        battleHud.SolarBeamIcon.Progress = player.AdvancedAbilityCd.FractionElapsed;
                    

        var shader = battleHud.TimerSprite.Material as ShaderMaterial;
        shader.SetShaderParameter("Progress", 1-battleWorld.EnemyWave.NextWaveTimer / battleWorld.EnemyWave.WaveTimeout);

        battleHud.TimerLabel.Text = battleWorld.EnemyWave.NextWaveTimer.ToString("N0");
    }
    
    /// <summary>
    /// Мы используем настолько альтернативный подход к расчету ТПС потому, что все значения физической дельты в движке - константы.
    /// Даже если реальный ТПС упадёт до 1, дельта, приходящая в _PhysicsProcess будет 1/60.
    /// </summary>
    [EventListener]
    public void OnBattleHudPhysicsProcessEvent(BattleHudPhysicsProcessEvent battleHudPhysicsProcessEvent)
    {
        BattleHud battleHud = battleHudPhysicsProcessEvent.BattleHud;
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
}