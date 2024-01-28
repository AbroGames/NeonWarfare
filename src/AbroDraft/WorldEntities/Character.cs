using System;
using Game.Content;
using Godot;
using MicroSurvivors;

namespace AbroDraft;

public partial class Character : Node2D
{
	public Sprite2D Sprite => GetNode("Sprite") as Sprite2D;

	public Vector2 Size
	{
		get => _size;
		set
		{
			_size = value;
			Sprite.SetAbsoluteScale(_size);
		}
	}
	
	private Vector2 _size = Vec(32);
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetSprite(GD.Load("res://Assets/Textures/Sprites/Square.png") as Texture2D);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	public void SetSprite(Texture2D texture)
	{
		Sprite.Texture = texture;
		Sprite.SetAbsoluteScale(_size);
	}
}