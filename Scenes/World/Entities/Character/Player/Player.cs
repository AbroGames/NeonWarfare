using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Scheduling;

namespace NeoVector;

public partial class Player : Character
{
	[Export] [NotNull] public Sprite2D ShieldSprite { get; private set; }
	
	public double PrimaryDamage { get; set; } = 1000;
	public double PrimaryDistance { get; set; } = 2000;
	public double UniversalDamageMultiplier { get; set; } = 1;
	
	public double SecondaryDamage { get; set; } = 5;
	public double SecondaryDistance { get; set; } = 1000;

	public Camera Camera;

	
	public Cooldown SecondaryCd { get; set; } = new(0.1, CooldownMode.Cyclic, true);

	public Cooldown BasicAbilityCd { get; set; } = new(6, CooldownMode.Single, true);
	public Cooldown AdvancedAbilityCd { get; set; } = new(50, CooldownMode.Single, true);
	
	//TODO serverInfo (mb ServerPlayer)?
	public Vector2 CurrentMovementVector { get; set; }
	public double CurrentMovementSpeed { get; set; }
	
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
		EventBus.Publish(new PlayerInputEvent(this, @event));
	}
}