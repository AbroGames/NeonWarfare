using Godot;

namespace KludgeBox.Events.Global;

public partial class BackButton : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed += () =>
		{
			/*var playerMenu = Root.Instance.PackedScenes.Screen.MainMenu.Instantiate<Control>();
		var menu = Root.Instance.Game.MenuContainer.GetCurrentStoredNode<Menu>();
		menu.changeMenu(playerMenu);*/ //TODO 
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}