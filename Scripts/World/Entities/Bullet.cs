using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Content;
using KludgeBox;

public partial class Bullet : Node2D
{

	[Export] public double Speed = 700; //pixels/sec
	[Export] public double RemainingDistance = 2000; //pixels

	public AuthorEnum Author;
	public int RemainingDamage = 1000;
	
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
					Audio2D.PlaySoundAt(Sfx.FuturisticHit, character.Position);
					character.HitFlash = 1;
					if (character.Hp >= RemainingDamage)
					{
						character.Hp -= RemainingDamage;
						QueueFree();
					}
					else
					{
						RemainingDamage -= character.Hp;
						character.Hp = 0;
						character.QueueFree();

						Root.Instance.WorldContainer.ClearStoredNode();
						
						var firstScene = References.Instance.FirstSceneBlueprint;
						Root.Instance.MenuContainer.ChangeStoredNode(firstScene.Instantiate() as Control);
					}
				}
			}
			
			if (body is Enemy enemy)
			{
				if (Author != AuthorEnum.ENEMY)
				{
					Audio2D.PlaySoundAt(Sfx.Hit, enemy.Position);
					enemy.HitFlash = 1;
					if (enemy.Hp >= RemainingDamage)
					{
						enemy.Hp -= RemainingDamage;
						QueueFree();
					}
					else
					{
						RemainingDamage -= enemy.Hp;
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
		RemainingDistance -= Speed * delta;
		if (RemainingDistance <= 0)
		{
			QueueFree();
		}
	}
	
	public enum AuthorEnum
	{
		PLAYER, ENEMY, ALLY
	}
}
