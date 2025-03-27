using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Screen.Components.TwoColoredBar;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.BattleWorld.ClientBattleWorld;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Screen.BattleHud;

public partial class BattleHud : Hud
{
	[ExportGroup("Other")]
	[Export] [NotNull] public Label WaveNumberLabel { get; private set; }
	[Export] [NotNull] public Label TimerLabel { get; private set; }
	[Export] [NotNull] public Label EnemiesCountLabel { get; private set; }
	
	
	public ClientBattleWorld ClientBattleWorld { get; set; }
	
	private readonly Stopwatch _physicsStopwatch = new();
	private readonly Queue<double> _deltas = new();
	
	public double FadeInTime { get; set; } = 0.5;
	public double HoldTime { get; set; } = 1;
	public double FadeOutTime { get; set; } = 0.5;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		//WaveMessageInitialPosition = WaveMessage.Position;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	}

	/// <summary>
	/// Мы используем настолько альтернативный подход к расчету ТПС потому, что все значения физической дельты в движке - константы.
	/// Даже если реальный ТПС упадёт до 1, дельта, приходящая в _PhysicsProcess будет 1/60.
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		var realDelta = _physicsStopwatch.Elapsed.TotalSeconds;
		WaveNumberLabel.Text = $"Wave: {ClientBattleWorld.CurrentWave}";
		TimerLabel.Text = $"Next wave in: {ClientBattleWorld.TimeToWave:N1} seconds";
		EnemiesCountLabel.Text = $"Enemies: {ClientBattleWorld.Enemies.Count}";
        
		_deltas.Enqueue(realDelta);
		if (_deltas.Count >= 120)
		{
			var tps = _deltas.Average();
			Tps.Text = $"TPS: {1/tps:N0}";
			_deltas.Dequeue();
		}
		_physicsStopwatch.Restart();
	}

	public override ClientWorld GetCurrentWorld()
	{
		return ClientBattleWorld;
	}

	public void PlayNewWaveEffect(int waveNumber)
	{
		//WaveMessage.Text = $"WAVE {waveNumber}";
		Tween colorTween = GetTree().CreateTween();
		Tween positionTween = GetTree().CreateTween();

		//colorTween.TweenProperty(WaveMessage, "modulate:a", 1f, FadeInTime);
		colorTween.TweenInterval(HoldTime);
		//colorTween.TweenProperty(WaveMessage, "modulate:a", 0f, FadeOutTime);
		
		//positionTween.TweenProperty(WaveMessage, "position:y", WaveMessageInitialPosition.Y + 50, FadeInTime);
		positionTween.TweenInterval(HoldTime);
		//positionTween.TweenProperty(WaveMessage, "position:y", WaveMessageInitialPosition.Y, FadeOutTime);
	}
}
