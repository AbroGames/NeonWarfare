using AbroDraft.Scripts.EventBus;
using Godot;
using KludgeBox;

namespace AbroDraft.Scenes.MainScenes.BattleWorld;

public partial class BattleWorldMainScene : Node2D
{

	[Export] [NotNull] public Scripts.Containers.NodeContainer WorldContainer { get; private set; }
	[Export] [NotNull] public Scripts.Containers.NodeContainer BackgroundContainer { get; private set; }
	[Export] [NotNull] public Scripts.Containers.NodeContainer HudContainer { get; private set; }
	[Export] [NotNull] public Scripts.Containers.NodeContainer MenuContainer { get; private set; }
	[Export] [NotNull] public Scripts.Containers.NodeContainer ForegroundContainer { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		EventBus.Publish(new BattleWorldMainSceneReadyEvent(this));
	}
}