using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;
using KludgeBox.Networking;
using NeonWarfare.Net;
using NeonWarfare.Utils;

namespace NeonWarfare;

public partial class Bullet : Node2D
{
	public enum AuthorEnum
	{
		PLAYER, ENEMY, ALLY
	}

	[Export] public double Speed = 700; //pixels/sec
	[Export] public double RemainingDistance = 2000; //pixels

	public Character Source { get; set; }
	public AuthorEnum Author;
	public double RemainingDamage = 1000;
	
	public static readonly ReadOnlyDictionary<AuthorEnum, Color> Colors = new Dictionary<AuthorEnum, Color>()
	{
		{ AuthorEnum.PLAYER, new Color(0, 1, 1)},
		{ AuthorEnum.ALLY, new Color(0, 1, 0)},
		{ AuthorEnum.ENEMY, new Color(1, 0, 0)}
			
	}.AsReadOnly();
	
	public override void _Ready()
	{
		if (!CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag)) //if is client
		{
			ReadyOnClient();
		}
		else
		{
			ReadyOnServer();
		}
	}
	
	public override void _Process(double delta)
	{
		Position += Vector2.FromAngle(Rotation - Mathf.Pi / 2) * (float) Speed * (float) delta;

		if (CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag)) //If is server
		{
			RemainingDistance -= Speed * delta;
			if (RemainingDistance <= 0)
			{
				QueueFree();
				long nid = ServerRoot.Instance.Game.World.NetworkEntityManager.RemoveEntity(this);
				Network.SendToAll(new ServerDestroyEntityPacket(nid));
			}
		}
	}

	private void ReadyOnClient()
	{
		Modulate = Colors[Author];
        		
		GetNode<Area2D>("Area2D").AreaEntered += area =>
		{
			if(area.GetParent() is not Character body) return;
        	
			if (body is Player player)
			{
				if (Author != AuthorEnum.PLAYER)
				{
					player.Camera.Punch(player.Position - Position, 10, 30);
					Audio2D.PlaySoundAt(Sfx.FuturisticHit, body.Position, 0.5f).PitchVariation(0.15f);
        			
					var hit = Fx.CreateBulletHitFx();
					hit.Modulate = Modulate;
					hit.Rotation = Rotation - Mathf.Pi / 2;
					hit.Scale =Scale;
					hit.Position = Position;
					GetParent().AddChild(hit);
				}
			}
        	
			if (body is Enemy enemy)
			{
				if (Author != AuthorEnum.ENEMY)
				{
					Audio2D.PlaySoundAt(Sfx.Hit, body.Position, 0.5f).PitchVariation(0.25f);
        			
					var hit = Fx.CreateBulletHitFx();
					hit.Modulate = Modulate;
					hit.Rotation = Rotation - Mathf.Pi / 2;
					hit.Scale = Scale;
					hit.Position = Position;
					GetParent().AddChild(hit);
				}
			}
		};
	}

	private void ReadyOnServer()
	{
		Modulate = Colors[Author];
        		
		GetNode<Area2D>("Area2D").AreaEntered += area =>
		{
			if(area.GetParent() is not Character body) return;
        	
			if (body is Player player)
			{
				if (Author != AuthorEnum.PLAYER)
				{
					ApplyDamage(player, new Color(0, 0, 0));
				}
			}
        	
			if (body is Enemy enemy)
			{
				if (Author != AuthorEnum.ENEMY)
				{
					ApplyDamage(enemy,new Color(1, 0, 0));
					float K = enemy.IsBoss ? 0.0025f : 0.025f;
					enemy.Position += Vector2.FromAngle(Rotation - Mathf.Pi / 2f) * (float) Speed * K;
				}
			}
		};
	}
	
	private void ApplyDamage(Character to, Color color)
	{
		if (RemainingDamage <= 0)
			return;
		
		var hp = to.Hp;
		to.TakeDamage(new Damage(Author, color, RemainingDamage, Source));
		RemainingDamage -= hp;

		if (RemainingDamage <= 0)
		{
			QueueFree();
			long nid = ServerRoot.Instance.Game.World.NetworkEntityManager.RemoveEntity(this);
			Network.SendToAll(new ServerDestroyEntityPacket(nid));
		}
	}
}