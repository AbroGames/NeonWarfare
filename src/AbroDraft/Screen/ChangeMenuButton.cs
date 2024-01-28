using Godot;
using System;
using KludgeBox;

public partial class ChangeMenuButton : Godot.Button
{
	[Export] private PackedScene _newMenu;
	
	public override void _Ready()
	{
		Pressed += OnClick;
	}

	private void OnClick()
	{
		var menu = References.Instance.MenuContainer.CurrentStoredNode as Menu;
		menu.changeMenu(_newMenu.Instantiate<Control>());
	}
}
