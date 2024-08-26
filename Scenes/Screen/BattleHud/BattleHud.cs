using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class BattleHud : Hud
{
	[ExportGroup("Bars")]
	[Export] [NotNull] public TwoColoredBar HpBar { get; private set; }
	
	[ExportGroup("Labels")]
	[Export] [NotNull] public Label Waves { get; private set; }
	[Export] [NotNull] public Label Enemies { get; private set; }
	[Export] [NotNull] public Label Level { get; private set; }
	
	[ExportGroup("FPS & TPS")]
	[Export] [NotNull] public Label Fps { get; private set; }
	[Export] [NotNull] public Label Tps { get; private set; }
	
	[ExportGroup("Other")]
	[Export] [NotNull] public Label WaveMessage { get; private set; }
	[Export] [NotNull] public Sprite2D TimerSprite { get; private set; }
	[Export] [NotNull] public Label TimerLabel { get; private set; }
	
	[ExportGroup("Abilities")]
	[Export] [NotNull] public Icon BeamIcon { get; private set; }
	[Export] [NotNull] public Icon SolarBeamIcon { get; private set; }
	
	public ClientBattleWorld ClientBattleWorld { get; set; }
	public Vector2 WaveMessageInitialPosition { get; set; }
	
	private readonly Stopwatch _physicsStopwatch = new();
	private readonly Queue<double> _deltas = new();
	
	public double FadeInTime { get; set; } = 0.5;
	public double HoldTime { get; set; } = 1;
	public double FadeOutTime { get; set; } = 0.5;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		WaveMessageInitialPosition = WaveMessage.Position;
	}

	public override void _Process(double delta)
	{
		Player player = ClientBattleWorld.Player;
		if (player == null) return;
		
		Waves.Text = $"Wave: {ClientBattleWorld.EnemyWave.WaveNumber}";
		Enemies.Text = $"Enemies: {ClientBattleWorld.Enemies.Count}";
        
		HpBar.CurrentUpperValue = player.Hp;
		double hpBarValueDelta = Mathf.Clamp(HpBar.CurrentLowerValue - HpBar.CurrentUpperValue, 
			0, Math.Max(HpBar.MaxValue - HpBar.CurrentUpperValue, 0));
		double hpBarValueDeltaDecrease = HpBar.MaxValue * 0.25 * delta;
		HpBar.CurrentLowerValue = player.Hp + hpBarValueDelta - hpBarValueDeltaDecrease;
        
		HpBar.MaxValue = player.MaxHp;
		HpBar.Label.Text = $"Health: {player.Hp:N0} / {player.MaxHp:N0}";
		Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";

		BeamIcon.Progress = player.BasicAbilityCd.FractionElapsed;
		SolarBeamIcon.Progress = player.AdvancedAbilityCd.FractionElapsed;
		
		var shader = TimerSprite.Material as ShaderMaterial;
		shader.SetShaderParameter("Progress", 1-ClientBattleWorld.EnemyWave.NextWaveTimer / ClientBattleWorld.EnemyWave.WaveTimeout);
		TimerLabel.Text = ClientBattleWorld.EnemyWave.NextWaveTimer.ToString("N0");
	}

	/// <summary>
	/// Мы используем настолько альтернативный подход к расчету ТПС потому, что все значения физической дельты в движке - константы.
	/// Даже если реальный ТПС упадёт до 1, дельта, приходящая в _PhysicsProcess будет 1/60.
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		var realDelta = _physicsStopwatch.Elapsed.TotalSeconds;
        
		_deltas.Enqueue(realDelta);
		if (_deltas.Count >= 120)
		{
			var tps = _deltas.Average();
			Tps.Text = $"TPS: {1/tps:N0}";
			_deltas.Dequeue();
		}
		_physicsStopwatch.Restart();
	}

	public void PlayNewWaveEffect(int waveNumber)
	{
		WaveMessage.Text = $"WAVE {waveNumber}";
		Tween colorTween = GetTree().CreateTween();
		Tween positionTween = GetTree().CreateTween();

		colorTween.TweenProperty(WaveMessage, "modulate:a", 1f, FadeInTime);
		colorTween.TweenInterval(HoldTime);
		colorTween.TweenProperty(WaveMessage, "modulate:a", 0f, FadeOutTime);
		
		positionTween.TweenProperty(WaveMessage, "position:y", WaveMessageInitialPosition.Y + 50, FadeInTime);
		positionTween.TweenInterval(HoldTime);
		positionTween.TweenProperty(WaveMessage, "position:y", WaveMessageInitialPosition.Y, FadeOutTime);
	}
}