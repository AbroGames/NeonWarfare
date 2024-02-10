using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Content;
using KludgeBox;
using Scenes.World;

public partial class Bullet : Node2D
{

	[Export] public double Speed = 700; //pixels/sec
	[Export] public double RemainingDistance = 2000; //pixels

	public Character Source { get; set; }
	public AuthorEnum Author;
	public double RemainingDamage = 1000;
	
	private Dictionary<AuthorEnum, Color> _colors = new Dictionary<AuthorEnum, Color>()
	{
		{ AuthorEnum.PLAYER, new Color(0, 1, 1)},
		{ AuthorEnum.ALLY, new Color(0, 1, 0)},
		{ AuthorEnum.ENEMY, new Color(1, 0, 0)}
			
	};
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
		GetNode<Sprite2D>("Sprite2D").Modulate = _colors[Author];
		
		GetNode<Area2D>("Area2D").AreaEntered += area =>
		{
			if(area.GetParent() is not Character body) return;
			
			if (body is Player player)
			{
				if (Author != AuthorEnum.PLAYER)
				{
					double damage = RemainingDamage;
					ApplyDamage(player, new Color(0, 0, 0));
					player.HpCanBeFastRegen += damage / 2;
					Audio2D.PlaySoundAt(Sfx.FuturisticHit, body.Position, 0.5f);
				}
			}
			
			if (body is Enemy enemy)
			{
				if (Author != AuthorEnum.ENEMY)
				{
					ApplyDamage(enemy,new Color(1, 0, 0));
					Audio2D.PlaySoundAt(Sfx.Hit, body.Position, 0.5f);
					double K = enemy.IsBoss ? 0.0025 : 0.025;
					enemy.Position += Vector2.FromAngle(Rotation - Mathf.Pi / 2) * Speed * K;
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

	private void ApplyDamage(Character to, Color color)
	{
		if (RemainingDamage <= 0)
			return;
		
		var hp = to.Hp;
		to.TakeDamage(new Damage(Author, color, RemainingDamage, Position, Source));
		RemainingDamage -= hp;
		

		if (RemainingDamage <= 0)
		{
			QueueFree();
		}
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
