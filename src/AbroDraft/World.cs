using Godot;
using System;

public partial class World : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var character = References.Instance.CharacterBlueprint.Instantiate() as Node2D;
		character.Position = Vec(100, 300);
		AddChild(character);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
