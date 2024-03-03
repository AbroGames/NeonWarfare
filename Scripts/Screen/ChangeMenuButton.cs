using Godot;

namespace NeoVector;

public partial class ChangeMenuButton : Button
{
	[Export] private PackedScene _newMenu;
	
	public override void _Ready()
	{
		Pressed += OnClick;
	}

	private void OnClick()
	{
		/*var menu = Root.Instance.Game.MenuContainer.GetCurrentStoredNode<Menu>();
	menu.changeMenu(_newMenu.Instantiate<Control>());*/ //TODO 
	}
}