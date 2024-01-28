using Godot;
using System;
using System.Diagnostics;
using KludgeBox;

public partial class Bullet : Node2D
{

	[Export] private double _speed = 700; //pixels/sec

	public AuthorEnum Author;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GetNode<Area2D>("Area2D").BodyEntered += body =>
		{
			if (body is Character character)
			{
				if (Author != AuthorEnum.PLAYER)
				{
					character.QueueFree();
					QueueFree();
				}
			}
			
			if (body is Enemy enemy)
			{
				if (Author != AuthorEnum.ENEMY)
				{
					enemy.QueueFree();
					QueueFree();
				}
			}
			
			if (body is Ally ally)
			{
				if (Author != AuthorEnum.ALLY)
				{
					ally.QueueFree();
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
	
	public enum AuthorEnum
	{
		PLAYER, ENEMY, ALLY
	}
}
