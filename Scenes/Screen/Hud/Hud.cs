using Godot;
using System;

public partial class Hud : Control
{
	
	[Export] [NotNull] public Label Hp { get; private set; }
	[Export] [NotNull] public Label Waves { get; private set; }
	[Export] [NotNull] public Label Enemies { get; private set; }

	public BattleWorld BattleWorld { get; set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	public override void _Process(double delta)
	{
		if (BattleWorld == null || !BattleWorld.IsNodeReady()) return;
		
		Hp.Text = $"HP: {BattleWorld.Player.Hp}/{BattleWorld.Player.MaxHp}";
		Waves.Text = $"Wave: {BattleWorld.WaveNumber}";
		Enemies.Text = $"Enemies: {BattleWorld.Enemies.Count}";
	}
}
