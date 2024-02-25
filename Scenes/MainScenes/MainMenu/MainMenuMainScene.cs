using AbroDraft.Scripts.EventBus;
using Godot;
using KludgeBox;

namespace AbroDraft.Scenes.MainScenes.MainMenu;

public partial class MainMenuMainScene : Node2D
{

	[Export] [NotNull] public Scripts.Containers.NodeContainer BackgroundContainer { get; private set; }
	[Export] [NotNull] public Scripts.Containers.NodeContainer MenuContainer { get; private set; }
	[Export] [NotNull] public Scripts.Containers.NodeContainer ForegroundContainer { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		EventBus.Publish(new MainMenuMainSceneReadyEvent(this));
	}
}