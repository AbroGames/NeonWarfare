using System;
using Game.Content;
using Godot;
using KludgeBox;
using KludgeBox.Scheduling;
using MicroSurvivors;
using Scenes.World;

public partial class Character : CharacterBody2D
{
	public event Action Died;
	public int MaxHp { get; protected set; } = 10000;
	public int Hp { get; set; } = 10000;
	protected Sprite2D Sprite => GetNode("Sprite2D") as Sprite2D;
	protected Vector2 SmoothedPosition => _smoothedPos;
	protected double HitFlash = 0;
	
	
	protected double _movementSpeed = 250; // in pixels/sec
	protected double _rotationSpeed = 300; // in degree/sec
	protected double _regenHpSpeed = 500; // hp/sec
	protected double _attackSpeed = 1; // attack/sec
	protected double _secToNextAttack = 0;
	
	private Vector2 _smoothedPos;


	public void TakeDamage(Damage damage)
	{
		HitFlash = 1;
		var appliedDamage = Mathf.Min(Hp, damage.Amount);
		Hp -= damage.Amount;
		
		if (Hp <= 0)
		{
			Died?.Invoke();
			QueueFree();
		}
		
		var dmgLabel =
			GD.Load<PackedScene>("res://Scenes/World/Entities/DamageLabel/DamageLabel.tscn")
				.Instantiate() as DamageLabel;
		
		dmgLabel.WithDamage(damage);
		dmgLabel.GlobalPosition = GlobalPosition;
		GetParent().AddChild(dmgLabel);
	}
	
	public virtual void Update(double delta)
	{
	}

	public virtual void PhysicsUpdate(double delta)
	{
		
	}

	public virtual void Init()
	{
		
	}

	/// <inheritdoc />
	public override void _Ready()
	{
		_smoothedPos = GlobalPosition;
		Init();
	}

	/// <inheritdoc />
	public override void _Process(double delta)
	{
		Update(delta);
		UpdateSmoothedPosition(delta);
		
		Sprite.GlobalPosition = _smoothedPos;
		// flash effect on hit processing
		HitFlash -= 0.02;
		HitFlash = Mathf.Max(HitFlash, 0);
		var shader = Sprite.Material as ShaderMaterial;
		shader.SetShaderParameter("colorMaskFactor", HitFlash);
	}

	/// <inheritdoc />
	public override void _PhysicsProcess(double delta)
	{
		PhysicsUpdate(delta);
	}

	private void UpdateSmoothedPosition(double delta)
	{
		var requiredMovement = GlobalPosition - _smoothedPos;

		var stepFactor = delta / (1.0 / 60);
		var smoothingFactor = 0.5;
		_smoothedPos += requiredMovement * stepFactor * smoothingFactor;
	}
}