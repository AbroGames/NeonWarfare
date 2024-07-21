using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class SafeWorldMainScene : Node2D, IWorldMainScene
{

	[Export] [NotNull] public SafeWorld SafeWorld { get; private set; }
	[Export] [NotNull] public SafeHud SafeHud { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		SafeHud.SafeWorld = SafeWorld;
		SafeWorld.SafeHud = SafeHud;
	}

	public World GetWorld()
	{
		return SafeWorld;
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