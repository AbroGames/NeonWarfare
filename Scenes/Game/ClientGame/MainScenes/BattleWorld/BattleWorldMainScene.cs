using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class BattleWorldMainScene : Node2D, IGameMainScene 
{
	
	[Export] [NotNull] public ClientBattleWorld ClientBattleWorld { get; private set; }
	[Export] [NotNull] public BattleHud BattleHud { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		BattleHud.ClientBattleWorld = ClientBattleWorld;
	}
	
	public ClientWorld GetWorld()
	{
		return ClientBattleWorld;
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