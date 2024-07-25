using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class BattleGameMainScene : Node2D, IGameMainScene //TODO split to server and client battleworld and safeworld. Or we don't need MainScene for server, we can use ServerBattleWorld only! 
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