using System;
using Game.Content;
using Godot;
using KludgeBox;
using Scenes.World;

public partial class Character : CharacterBody2D
{
	[Export] [NotNull] public Sprite2D Sprite { get; private set; }
	[Export] [NotNull] public CollisionShape2D CollisionShape { get; private set; }
	
	public event Action Died;
	
	public double MaxHp { get; set; } = 10000;
	public double Hp { get; set; } = 10000;
	public double RegenHpSpeed { get; set; } = 500; // hp/sec
	public double MovementSpeed { get; set; } = 250; // in pixels/sec
	public double RotationSpeed { get; set; } = 300; // in degree/sec
	
	public double AttackSpeed { get; set; } = 1; // attack/sec
	public double SecToNextAttack { get; set; } = 0;
	
	protected double HitFlash = 0;

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Root.Instance.EventBus.Publish(new CharacterReadyEvent(this));
	}

	public void TakeDamage(Damage damage)
	{
		if (Hp <= 0) return;
		
		HitFlash = 1;
		var appliedDamage = Mathf.Min(Hp, damage.Amount);
		Hp -= damage.Amount;
		
		if (Hp <= 0)
		{
			if (damage.Source is Player ply && this is Enemy enemy)
			{
				
				if (enemy.IsBoss)
				{
					int orbs = 10;
					int xpPerOrb = enemy.BaseXp / orbs;
					for (int i = 0; i < orbs; i++)
					{
						var orb = XpOrb.Create();
						orb.Position = Position;
						orb.Configure(ply, xpPerOrb);
						GetParent().AddChild(orb);
					}
				}
				else
				{
					var orb = XpOrb.Create();
					orb.Position = Position;
					orb.Configure(ply, enemy.BaseXp);
					GetParent().AddChild(orb);
				}
			}
			
			Died?.Invoke();

			Audio2D.PlaySoundAt(Sfx.FuturisticCrack, GlobalPosition);
			QueueFree();
		}
		
		var dmgLabel = FloatingLabel.Create();
		
		if(appliedDamage <= 0)
			Log.Debug(appliedDamage.ToString("N0"));
			
		dmgLabel.Configure(appliedDamage.ToString("N0"), damage.LabelColor, Mathf.Max(Math.Log(appliedDamage, 75), 0.8));
		dmgLabel.Position = damage.Position;
		GetParent().AddChild(dmgLabel);
	}
	
	public override void _Process(double delta)
	{
		Root.Instance.EventBus.Publish(new CharacterProcessEvent(this, delta));
		// flash effect on hit processing
		HitFlash -= 0.02;
		HitFlash = Mathf.Max(HitFlash, 0);
		var shader = Sprite.Material as ShaderMaterial;
		shader.SetShaderParameter("colorMaskFactor", HitFlash);
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Root.Instance.EventBus.Publish(new CharacterPhysicsProcessEvent(this, delta));
	}
}