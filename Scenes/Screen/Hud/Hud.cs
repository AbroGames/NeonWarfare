using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;


public partial class Hud : Control
{
	[Export] [NotNull] public ProgressBar Xp { get; private set; }
	[Export] [NotNull] public HpBar HpBar { get; private set; }
	[Export] [NotNull] public Label XpLabel { get; private set; }
	//[Export] [NotNull] public Label Hp { get; private set; }
	[Export] [NotNull] public Label Waves { get; private set; }
	[Export] [NotNull] public Label Enemies { get; private set; }
	[Export] [NotNull] public Label Level { get; private set; }
	[Export] [NotNull] public Label WaveMessage { get; private set; }
	[Export] [NotNull] public Label Fps { get; private set; }

	public BattleWorld BattleWorld { get; set; }

	private Vector2 _waveMessageInitialPosition;

	private Queue<double> _deltas = new();
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		_waveMessageInitialPosition = WaveMessage.Position;
		CallDeferred(nameof(Subscribe));
	}

	public override void _Process(double delta)
	{
		if (BattleWorld == null || !BattleWorld.IsNodeReady()) return;

		Xp.Value = (double) BattleWorld.Player.Xp / 10000; //TODO
		XpLabel.Text = $"Level: {BattleWorld.Player.Level}";
		XpLabel.TooltipText = $"Xp: {BattleWorld.Player.Xp} / 10000"; //TODO
		Level.Text = $"Level: {BattleWorld.Player.Level}";
		
		//Hp.Text = $"HP: {BattleWorld.Player.Hp:N0}/{BattleWorld.Player.MaxHp:N0}";
		Waves.Text = $"Wave: {BattleWorld.WaveNumber}";
		Enemies.Text = $"Enemies: {BattleWorld.Enemies.Count}";

		HpBar.Hp = BattleWorld.Player.Hp;
		HpBar.MaxHp = BattleWorld.Player.MaxHp;
		
		_deltas.Enqueue(delta);
		if (_deltas.Count >= 240)
		{
			_deltas.Dequeue();
			var fps = 1 / _deltas.Average();
			Fps.Text = $"FPS: {fps:N0}";
		}
	}

	private void Subscribe()
	{
		BattleWorld.NewWave += ShowWaveMessage;
	}
	private void ShowWaveMessage()
	{
		WaveMessage.Text = $"WAVE {BattleWorld.WaveNumber}";
		var colorTween = GetTree().CreateTween();
		var positionTween = GetTree().CreateTween();

		double fadeInTime = 0.5;
		double holdTime = 1;
		double fadeOutTime = 0.5;

		colorTween.TweenProperty(WaveMessage, "modulate:a", 1f, fadeInTime);
		colorTween.TweenInterval(holdTime);
		colorTween.TweenProperty(WaveMessage, "modulate:a", 0f, fadeOutTime);
		
		positionTween.TweenProperty(WaveMessage, "position", _waveMessageInitialPosition + Vec(0,50), fadeInTime);
		positionTween.TweenInterval(holdTime);
		positionTween.TweenProperty(WaveMessage, "position", _waveMessageInitialPosition, fadeOutTime);
	}
}
