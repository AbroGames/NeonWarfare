using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class SafeWorldMainScene : WorldMainScene
{

	[Export] [NotNull] public SafeWorld SafeWorld { get; private set; }
	[Export] [NotNull] public SafeHud SafeHud { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);

		World = SafeWorld;
		Hud = SafeHud;
		
		SafeHud.SafeWorld = SafeWorld;
		SafeWorld.SafeHud = SafeHud;
	}
}