using System.Collections.Generic;
using AbroDraft.Scripts.Content;
using Godot;

namespace AbroDraft.Scenes.World.Entities.Bullet;

public partial class Bullet : Node2D
{

	[Export] public double Speed = 700; //pixels/sec
	[Export] public double RemainingDistance = 2000; //pixels

	public Character.Character Source { get; set; }
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
		
		Modulate = _colors[Author];
		
		GetNode<Area2D>("Area2D").AreaEntered += area =>
		{
			if(area.GetParent() is not Character.Character body) return;
			
			if (body is Character.Player.Player player)
			{
				if (Author != AuthorEnum.PLAYER)
				{
					double damage = RemainingDamage;
					ApplyDamage(player, new Color(0, 0, 0));
					player.Camera.Punch(player.Position - Position, 10, 30);
					Audio2D.PlaySoundAt(Sfx.FuturisticHit, body.Position, 0.5f).PitchVariation(0.15f);
					
					var hit = Fx.CreateBulletHitFx();
					hit.Modulate = Modulate;
					hit.Rotation = Rotation - Mathf.Pi / 2;
					hit.Scale = Scale;
					hit.Position = Position;
					GetParent().AddChild(hit);
				}
			}
			
			if (body is Character.Enemy.Enemy enemy)
			{
				if (Author != AuthorEnum.ENEMY)
				{
					ApplyDamage(enemy,new Color(1, 0, 0));
					Audio2D.PlaySoundAt(Sfx.Hit, body.Position, 0.5f).PitchVariation(0.25f);
					double K = enemy.IsBoss ? 0.0025 : 0.025;
					enemy.Position += Vector2.FromAngle(Rotation - Mathf.Pi / 2) * Speed * K;
					
					var hit = Fx.CreateBulletHitFx();
					hit.Modulate = Modulate;
					hit.Rotation = Rotation - Mathf.Pi / 2;
					hit.Scale = Scale;
					hit.Position = Position;
					GetParent().AddChild(hit);
				}
			}
			
			if (body is Character.Ally.Ally ally)
			{
				if (Author != AuthorEnum.ALLY)
				{
					
				}
			}
		};
	}

	private void ApplyDamage(Character.Character to, Color color)
	{
		if (RemainingDamage <= 0)
			return;
		
		var hp = to.Hp;
		to.TakeDamage(new Damage(Author, color, RemainingDamage, Source));
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