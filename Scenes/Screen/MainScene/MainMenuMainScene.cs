using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.Screen.MainScene;

public partial class MainMenuMainScene : Node2D
{

	[Export] [NotNull] public NodeContainer BackgroundContainer { get; private set; }
	[Export] [NotNull] public Background3D Background3D { get; private set; }
	[Export] [NotNull] public NodeContainer MenuContainer { get; private set; }
	[Export] [NotNull] public NodeContainer ForegroundContainer { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	public void ChangeMenu(PackedScene newMenu)
	{
		MenuContainer.ChangeStoredNode(newMenu.Instantiate());
	}
}
