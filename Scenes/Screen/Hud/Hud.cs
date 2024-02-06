using Godot;
using System;

public partial class Hud : Control
{
	
	[Export] [NotNull] public ProgressBar Xp { get; private set; }
	[Export] [NotNull] public Label XpLabel { get; private set; }
	[Export] [NotNull] public Label Hp { get; private set; }
	[Export] [NotNull] public Label Waves { get; private set; }
	[Export] [NotNull] public Label Enemies { get; private set; }
	[Export] [NotNull] public Label Level { get; private set; }

	public BattleWorld BattleWorld { get; set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	public override void _Process(double delta)
	{
		if (BattleWorld == null || !BattleWorld.IsNodeReady()) return;

		Xp.Value = (double)BattleWorld.Player.Xp / BattleWorld.Player.RequiredXp;
		XpLabel.Text = $"{BattleWorld.Player.Xp} / {BattleWorld.Player.RequiredXp}";
		Level.Text = $"Level: {BattleWorld.Player.Level}";
		
		Hp.Text = $"HP: {BattleWorld.Player.Hp:N0}/{BattleWorld.Player.MaxHp:N0}";
		Waves.Text = $"Wave: {BattleWorld.WaveNumber}";
		Enemies.Text = $"Enemies: {BattleWorld.Enemies.Count}";
	}
}
