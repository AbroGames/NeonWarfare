using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;

namespace NeoVector.World;

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
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		EventBus.Publish(new BulletReadyEvent(this));
	}

	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		EventBus.Publish(new BulletProcessEvent(this, delta));
	}
	
	
}