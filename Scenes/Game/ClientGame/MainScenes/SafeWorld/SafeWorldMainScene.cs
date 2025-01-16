using Godot;
using NeonWarfare.Scenes.Screen;
using NeonWarfare.Scenes.Screen.SafeHud;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.SafeWorld.ClientSafeWorld;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Game.ClientGame.MainScenes.SafeWorld;

public partial class SafeWorldMainScene : Node2D, IWorldMainScene
{

	[Export] [NotNull] public ClientSafeWorld ClientSafeWorld { get; private set; }
	[Export] [NotNull] public SafeHud SafeHud { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		SafeHud.ClientSafeWorld = ClientSafeWorld;
	}

	public ClientWorld GetWorld()
	{
		return ClientSafeWorld;
	}

	public Hud GetHud()
	{
		return SafeHud;
	}
	
	public Node2D GetAsNode2D()
	{
		return this;
	}
}
