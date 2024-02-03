using Godot;
using System;

public partial class Root : Node2D
{
	
	[Export] [NotNull] public Node2DContainer WorldContainer { get; private set; }
	[Export] [NotNull] public ControlContainer BackgroundContainer { get; private set; }
	[Export] [NotNull] public ControlContainer HudContainer { get; private set; }
	[Export] [NotNull] public ControlContainer MenuContainer { get; private set; }
	[Export] [NotNull] public ControlContainer ForegroundContainer { get; private set; }
	
	[Export] [NotNull] public WorldPackedScenesContainer WorldPackedScenesContainer { get; private set; }
	[Export] [NotNull] public ScreenPackedScenesContainer ScreenPackedScenesContainer { get; private set; }
	
	[Export] [NotNull] public PlayerInfo PlayerInfo { get; private set; }
	
	public static Root Instance { get; private set; }

	public override void _EnterTree()
	{
		base._EnterTree();
		Instance = this;
	}

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}
}
