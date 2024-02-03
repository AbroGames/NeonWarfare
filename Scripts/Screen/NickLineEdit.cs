using Godot;
using System;

public partial class NickLineEdit : LineEdit
{
	public override void _Ready()
	{
		Text = Root.Instance.PlayerInfo.playerName;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
