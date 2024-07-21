using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class BattleWorldMainScene : Node2D, IWorldMainScene
{
	
	[Export] [NotNull] public BattleWorld BattleWorld { get; private set; }
	[Export] [NotNull] public BattleHud BattleHud { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		BattleHud.BattleWorld = BattleWorld;
		BattleWorld.BattleHud = BattleHud;
	}
	
	public World GetWorld()
	{
		return BattleWorld;
	}

	public Hud GetHud()
	{
		return BattleHud;
	}

	public Node2D GetAsNode2D()
	{
		return this;
	}
}