using Godot;
using System;

public partial class Menu : Control
{
	public override void _Ready()
	{
		var mainMenu = References.Instance.MainMenu.Instantiate() as Control;
		AddChild(mainMenu);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
