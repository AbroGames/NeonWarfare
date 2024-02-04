using Godot;
using System;

public partial class BattleWorldMainScene : Node2D
{

	[Export] [NotNull] public NodeContainer WorldContainer { get; private set; }
	[Export] [NotNull] public NodeContainer BackgroundContainer { get; private set; }
	[Export] [NotNull] public NodeContainer HudContainer { get; private set; }
	[Export] [NotNull] public NodeContainer MenuContainer { get; private set; }
	[Export] [NotNull] public NodeContainer ForegroundContainer { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);

		Hud hud = HudContainer.GetCurrentStoredNode<Hud>();
		hud.BattleWorld = WorldContainer.GetCurrentStoredNode<BattleWorld>();
	}

	public override void _Process(double delta)
	{
	}
}
