using System;
using Godot;
using KludgeBox;
using KludgeBox.Scheduling;

namespace KludgeBox.Events.Global.World;

public partial class Character : CharacterBody2D
{
	[Export] [NotNull] public Sprite2D Sprite { get; private set; }
	[Export] [NotNull] public CollisionShape2D CollisionShape { get; private set; }
	[Export] [NotNull] public Smoothing2D Smoother { get; private set; }
	
	public double MaxHp { get; set; } = 10000;
	public double Hp { get; set; } = 10000;
	public double RegenHpSpeed { get; set; } = 100; // hp/sec
	public double MovementSpeed { get; set; } = 250; // in pixels/sec
	public double RotationSpeed { get; set; } = 300; // in degree/sec
	
	public Cooldown PrimaryCd { get; set; } = new(1/3d, CooldownMode.Single);
	
	internal double HitFlash = 0;

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		EventBus.Publish(new CharacterReadyEvent(this));
	}

	public virtual void Die()
	{
		EventBus.Publish(new CharacterDeathEvent(this));
	}

	public void TeleportTo(Vector2 pos)
	{
		Position = pos;
		SkipSmoothing();
	}

	public void SkipSmoothing()
	{
		Smoother.Teleport();
	}

	public virtual void TakeDamage(Damage damage)
	{
		EventBus.Publish(new CharacterApplyDamageRequest(this, damage));
	}
	
	public override void _Process(double delta)
	{
		EventBus.Publish(new CharacterProcessEvent(this, delta));
	}
	
	public override void _PhysicsProcess(double delta)
	{
		EventBus.Publish(new CharacterPhysicsProcessEvent(this, delta));
	}
}