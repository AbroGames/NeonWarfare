using Godot;
using System;
using System.Diagnostics;
using KludgeBox;

public partial class Bullet : Node2D
{

	[Export] private double _speed = 700; //pixels/sec

	public bool AuthorIsPlayer;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<Area2D>("Area2D").BodyEntered += body =>
		{
			if (body is Character character)
			{
				if (!AuthorIsPlayer)
				{
					character.QueueFree();
					QueueFree();
				}
			}
			
			if (body is Enemy enemy)
			{
				if (AuthorIsPlayer)
				{
					enemy.QueueFree();
					QueueFree();
				}
			}
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Position += Vector2.FromAngle(Rotation - Mathf.Pi / 2) * _speed * delta;
	}
}
