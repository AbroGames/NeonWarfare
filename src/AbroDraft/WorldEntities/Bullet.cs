using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Content;
using KludgeBox;

public partial class Bullet : Node2D
{

	[Export] public double Speed = 700; //pixels/sec
	[Export] private double _remainingDistance = 2000; //pixels

	public AuthorEnum Author;
	private int _remainingDamage = 1000;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Dictionary<AuthorEnum, Color> colors = new Dictionary<AuthorEnum, Color>()
		{
			{ AuthorEnum.PLAYER, new Color(0, 1, 1)},
			{ AuthorEnum.ALLY, new Color(0, 1, 0)},
			{ AuthorEnum.ENEMY, new Color(1, 0, 0)}
			
		};
		GetNode<Sprite2D>("Sprite2D").Modulate = colors[Author];
		
		GetNode<Area2D>("Area2D").BodyEntered += body =>
		{
			if (body is Character character)
			{
				if (Author != AuthorEnum.PLAYER)
				{
					Audio2D.PlaySoundAt(Sfx.Hit, character.Position);
					character.HitFlash = 1;
					if (character.Hp >= _remainingDamage)
					{
						character.Hp -= _remainingDamage;
						QueueFree();
					}
					else
					{
						_remainingDamage -= character.Hp;
						character.Hp = 0;
						character.QueueFree();

						References.Instance.WorldContainer.ClearStoredNode();
						
						var firstScene = References.Instance.FirstSceneBlueprint;
						References.Instance.MenuContainer.ChangeStoredNode(firstScene.Instantiate() as Control);
					}
				}
			}
			
			if (body is Enemy enemy)
			{
				if (Author != AuthorEnum.ENEMY)
				{
					Audio2D.PlaySoundAt(Sfx.Hit, enemy.Position);
					if (enemy.Hp >= _remainingDamage)
					{
						enemy.Hp -= _remainingDamage;
						QueueFree();
					}
					else
					{
						_remainingDamage -= enemy.Hp;
						enemy.Hp = 0;
						enemy.QueueFree();
					}
				}
			}
			
			if (body is Ally ally)
			{
				if (Author != AuthorEnum.ALLY)
				{
					
				}
			}
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Position += Vector2.FromAngle(Rotation - Mathf.Pi / 2) * Speed * delta;
		_remainingDistance -= Speed * delta;
		if (_remainingDistance <= 0)
		{
			QueueFree();
		}
	}
	
	public enum AuthorEnum
	{
		PLAYER, ENEMY, ALLY
	}
}
