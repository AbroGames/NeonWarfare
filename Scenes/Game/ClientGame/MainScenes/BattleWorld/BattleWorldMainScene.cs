using Godot;
using NeonWarfare.Scenes.Screen;
using NeonWarfare.Scenes.Screen.BattleHud;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.BattleWorld.ClientBattleWorld;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Game.ClientGame.MainScenes.BattleWorld;

public partial class BattleWorldMainScene : Node2D, IWorldMainScene 
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
