using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class SafeWorldMainScene : Node2D
{

	[Export] [NotNull] public SafeWorld World { get; private set; }
	[Export] [NotNull] public SafeHud Hud { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		Hud.SafeWorld = World;
		World.SafeHud = Hud;
	}
}