using Godot;

namespace AbroDraft.Scenes.Screen.Menu.ProfileButton;

public partial class ProfileButton : Button
{
	public override void _Ready()
	{
		Pressed += () =>
		{
			/*var playerMenu = Root.Instance.PackedScenes.Screen.ProfileMenu.Instantiate<Control>();
		var menu = Root.Instance.Game.MenuContainer.GetCurrentStoredNode<Menu>();
		menu.changeMenu(playerMenu);*/ //TODO 
		};
	}

}