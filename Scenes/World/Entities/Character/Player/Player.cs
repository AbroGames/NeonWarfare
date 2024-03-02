using System;
using Godot;
using KludgeBox;
using KludgeBox.Scheduling;

namespace NeoVector.World;

public partial class Player : Character
{
	[Export] [NotNull] public Sprite2D ShieldSprite { get; private set; }
	
	public long Xp { get; set; }
	public long NextLevelXp { get; set; }
	public double RequiredXpLevelFactor { get; set; } = 1.5;
	public int BasicRequiredXp { get; set; } = 10;
	public int Level { get; set; } = 1;
	
	public double PrimaryDamage { get; set; } = 1000;
	public double PrimaryDistance { get; set; } = 2000;
	public double UniversalDamageMultiplier { get; set; } = 1;
	
	public double SecondaryDamage { get; set; } = 5;
	public double SecondaryDistance { get; set; } = 1000;

	public Camera Camera;

	
	public Cooldown SecondaryCd { get; set; } = new(0.1);

	public Cooldown BasicAbilityCd { get; set; } = new(6, CooldownMode.Single, true);
	public Cooldown AdvancedAbilityCd { get; set; } = new(50, CooldownMode.Single, true);
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		EventBus.Publish(new PlayerReadyEvent(this));
	}

	/// <inheritdoc />
	public override void Die()
	{
		base.Die();
		EventBus.Publish(new PlayerDeathEvent(this));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);
		EventBus.Publish(new PlayerProcessEvent(this, delta));
	}

	public override void _PhysicsProcess(double delta)
	{
		EventBus.Publish(new PlayerPhysicsProcessEvent(this, delta));
	}


	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed(Keys.AbilityBasic))
			if (BasicAbilityCd.Use())
			{
				EventBus.Publish(new PlayerBasicSkillUseEvent(this));
			}
		
		if (@event.IsActionPressed(Keys.AbilityAdvanced))
			if (AdvancedAbilityCd.Use())
			{
				EventBus.Publish(new PlayerAdvancedSkillUseEvent(this));
			}

		if (@event.IsActionPressed(Keys.WheelUp))
		{
			EventBus.Publish(new PlayerMouseWheelInputEvent(this, WheelEventType.WheelUp));
		}

		if (@event.IsActionPressed(Keys.WheelDown))
		{
			EventBus.Publish(new PlayerMouseWheelInputEvent(this, WheelEventType.WheelDown));
		}
	}
}