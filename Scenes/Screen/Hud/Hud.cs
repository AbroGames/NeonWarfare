using Godot;
using System;

public partial class Hud : Control
{
	
	[Export] [NotNull] public Label Hp { get; private set; }
	[Export] [NotNull] public Label Waves { get; private set; }
	[Export] [NotNull] public Label Enemies { get; private set; }

	public World World;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	public override void _Process(double delta)
	{
		if (World == null || !World.IsValid()) return;
		
		Hp.Text = $"HP: {World.Player.Hp}/{World.Player.MaxHp}";
		Waves.Text = $"Wave: {World.WaveNumber}";
		Enemies.Text = $"Enemies: {World.Enemies.Count}";
	}
}
