using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class SafeWorldMainScene : Node2D, IGameMainScene
{

	[Export] [NotNull] public ClientSafeWorld ClientSafeWorld { get; private set; }
	[Export] [NotNull] public SafeHud SafeHud { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		SafeHud.ClientSafeWorld = ClientSafeWorld;
		ClientSafeWorld.SafeHud = SafeHud;
	}

	public World GetWorld()
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