using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AbroDraft.Scenes.World.Entities.Character.Player;
using AbroDraft.Scripts.EventBus;
using AbroDraft.Scripts.Utils;
using Godot;

namespace AbroDraft.Scenes.Screen.BattleHud;

[GameService]
public class BattleHudService
{

    private readonly Stopwatch _physicsStopwatch = new();
    private readonly Queue<double> _deltas = new();
    
    public BattleHudService()
    {
        EventBus.Subscribe<BattleHudProcessEvent>(OnBattleHudProcessEvent);
        EventBus.Subscribe<BattleHudPhysicsProcessEvent>(OnBattleHudPhysicsProcessEvent);
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
    public void UpdateHudPhysics(BattleHud battleHud, World.BattleWorld.BattleWorld battleWorld)
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

    public void UpdateBattleHud(BattleHud battleHud, World.BattleWorld.BattleWorld battleWorld)
    {
        World.Entities.Character.Player.Player player = battleWorld.Player;
        
        //TODO хочу такую запись: int playerRequiredXp = EventBus.PublishQuery<int>(new PlayerGetRequiredXpQuery(player));
        //PlayerGetRequiredXpQuery playerGetRequiredXpQuery = new PlayerGetRequiredXpQuery(player);
        //EventBus.Publish(playerGetRequiredXpQuery);
        int playerRequiredXp = EventBus.Require(new PlayerGetRequiredXpQuery(player));//playerGetRequiredXpQuery.Response;
        
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

        battleHud.BeamIcon.Progress = player.BasicAbilityCd.FractionElapsed;
        battleHud.SolarBeamIcon.Progress = player.AdvancedAbilityCd.FractionElapsed;
                    

        var shader = battleHud.TimerSprite.Material as ShaderMaterial;
        shader.SetShaderParameter("Progress", 1-battleWorld.EnemyWave.NextWaveTimer / battleWorld.EnemyWave.WaveTimeout);

        battleHud.TimerLabel.Text = battleWorld.EnemyWave.NextWaveTimer.ToString("N0");
    }

    
}