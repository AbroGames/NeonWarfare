using Godot;
using KludgeBox;

namespace KludgeBox.Events.Global;

public partial class BattleWorldMainScene : Node2D
{

	[Export] [NotNull] public NodeContainer WorldContainer { get; private set; }
	[Export] [NotNull] public NodeContainer BackgroundContainer { get; private set; }
	[Export] [NotNull] public NodeContainer HudContainer { get; private set; }
	[Export] [NotNull] public NodeContainer MenuContainer { get; private set; }
	[Export] [NotNull] public NodeContainer ForegroundContainer { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		EventBus.Publish(new BattleWorldMainSceneReadyEvent(this));
	}
}