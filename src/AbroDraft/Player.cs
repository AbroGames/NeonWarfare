using Godot;
using System;

public partial class Player : Node2D
{
	public string playerName;
	public Color playerColor;
	public override void _Ready()
	{
		playerName = "Player";
		playerColor = new Color();
	}

	public override void _Process(double delta)
	{
	}
}
