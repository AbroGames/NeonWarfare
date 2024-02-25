using Godot;

namespace AbroDraft.Scripts.Screen;

public partial class Menu : Control
{
	private Control _currentMenu;
	public override void _Ready()
	{
		var mainMenu = Scenes.Root.Root.Instance.PackedScenes.Screen.MainMenu.Instantiate() as Control;
		_currentMenu = mainMenu;
		AddChild(mainMenu);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void changeMenu(Control newMenu)
	{
		_currentMenu.QueueFree();
		_currentMenu = newMenu;
		AddChild(newMenu);
	}
}