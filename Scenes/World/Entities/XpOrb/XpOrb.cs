using Godot;
using System;
using Game.Content;
using KludgeBox;

public partial class XpOrb : Node2D
{
	[Export] [NotNull] public Trail Trail { get; private set; }
	
	public int Xp { get; private set; }
	public Player Target { get; private set; }


	private double MaxSpeed => Target.MovementSpeed * 2.5;
	private static double _speedUp = 500;
	private double _speed;
	private Vector2 _initialVelocity;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		_initialVelocity = Rand.UnitVector * 500;
		Trail.Reset();
	}

	public void Configure(Player target, int xp)
	{
		Target = target;
		Xp = xp;
	}
	
	public override void _Process(double delta)
	{
		_speed += _speedUp * delta;
		_initialVelocity = _initialVelocity.MoveToward(Vec(), _speedUp * delta);
		_speed = Mathf.Min(_speed, MaxSpeed);
		var dist = this.DistanceTo(Target);
		var movement = Mathf.Min(_speed*delta, dist);

		var dir = this.DirectionTo(Target);
		Position += dir * movement + _initialVelocity * delta;

		if (dist < _speed * delta)
		{
			var dummy = Trail.Drop();
			dummy.Modulate = Modulate;
			Trail.Target = Trail.GetParent() as Node2D;
			Trail.Destruct(Trail.Length);
			Root.Instance.EventBus.Publish(new PlayerGainXpEvent(Target, Xp));
			
			var label = FloatingLabel.Create($"+{Xp}", Modulate, 0.6);
			label.Position = Position;
			GetParent().AddChild(label);

			Audio2D.PlaySoundAt(Sfx.Beep, Position, 0.5f);
			
			QueueFree();
		}
	}

	public static XpOrb Create()
	{
		return Root.Instance.PackedScenes.World.XpOrb.Instantiate<XpOrb>();
	}
}
