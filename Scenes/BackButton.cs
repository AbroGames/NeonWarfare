using Godot;
using System;

public partial class BackButton : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed += () =>
		{
			var playerMenu = GD.Load<PackedScene>("res://Scenes/MainMenu.tscn").Instantiate<Control>();
			var menu = References.Instance.MenuContainer.CurrentStoredNode as Menu;
			menu.changeMenu(playerMenu);
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
