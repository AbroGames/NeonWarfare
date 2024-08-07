using System;
using Godot;
using KludgeBox;
using KludgeBox.Networking;
using KludgeBox.Scheduling;
using NeonWarfare.Net;
using NeonWarfare.Utils;

namespace NeonWarfare;

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

	private const double cdBase = 1/3d;
	public Cooldown PrimaryCd { get; set; } = new(cdBase/2, CooldownMode.Single, true);
	
	
	internal double HitFlash = 0;

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	public virtual void Die() { }

	public void TeleportTo(Vector2 pos)
	{
		Position = pos;
		SkipSmoothing();
	}

	public void SkipSmoothing()
	{
		Smoother.Teleport();
	}

	public void TakeDamage(Damage damage)
	{
		if (Hp <= 0) return;
    		
		HitFlash = 1;
		var appliedDamage = Mathf.Min(Hp, damage.Amount);
		Hp -= damage.Amount;
    		
		if (Hp <= 0)
		{
			Die();
			
			if (!CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag)) //If is client
			{			
				var deathDummy = this.DropDummy();
				var derbisDummy = this.DropDummy();
				var death = Fx.CreateDeathFx();
				var derbis = Fx.CreateDebrisFx();
				derbis.Modulate = Sprite.Modulate;
				deathDummy.AddChild(death);
				derbisDummy.AddChild(derbis);
				//GetParent().MoveChild(derbisDummy, GetIndex() - 1);
				derbisDummy.ToBackground();
    				
				Audio2D.PlaySoundAt(Sfx.FuturisticCrack, GlobalPosition).PitchVariation(0.25f);
			}
			QueueFree();
		}
    		
		var dmgLabel = FloatingLabel.Create();
		
		if (appliedDamage <= 0) Log.Debug(appliedDamage.ToString("N0"));
    			
		dmgLabel.Configure(appliedDamage.ToString("N0"), damage.LabelColor, Mathf.Max(Math.Log(appliedDamage, 20), 0.8));
		dmgLabel.Position = Position + Rand.InsideUnitCircle * 50;
		GetParent().AddChild(dmgLabel);
	}
	
	public override void _Process(double delta)
	{
		// flash effect on hit processing
		HitFlash -= 100 * delta;
		HitFlash = Mathf.Max(HitFlash, 0);
		var shader = Sprite.Material as ShaderMaterial;
		shader.SetShaderParameter("colorMaskFactor", HitFlash);
		PrimaryCd.Update(delta);
		
		
		if (Input.IsActionPressed(Keys.AttackPrimary) && PrimaryCd.Use())
		{
			Shoot();
		}
	}

	/// <inheritdoc />
	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed(Keys.AttackPrimary))
		{
			if (PrimaryCd.Use())
			{
				Shoot();
			}
		}
	}

	public virtual void Shoot()
	{
		
	}
}