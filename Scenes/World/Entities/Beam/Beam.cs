using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Scheduling;

namespace NeoVector;

public partial class Beam : Node2D
{
	public Player Source { get; set; }
	public double Dps { get; set; } = 6000;
	public double PushVel { get; set; } = 1000;
	[Export] [NotNull] public Area2D OuterHitArea { get; private set; }
	[Export] [NotNull] public Sprite2D OuterSpawnSprite { get; set; }
	[Export] [NotNull] public Sprite2D OuterBeamSprite { get; set; }
	
	[Export] [NotNull] public Area2D InnerHitArea { get; private set; }
	[Export] [NotNull] public Sprite2D InnerSpawnSprite { get; set; }
	[Export] [NotNull] public Sprite2D InnerBeamSprite { get; set; }
	[Export] [NotNull] public Curve SizeCurve { get; set; }
	[Export] [NotNull] public CpuParticles2D Particles { get; set; }

	public ManualShake Shaker;
	
	internal double Ttl = 2;
	internal double StartTtl;
	internal double InnerStartWidth;
	internal double OuterStartWidth;
	internal double Ang;
	internal float StartGlow;
	internal double ShakeDist = 1500;
	internal Cooldown DamageCd = new(duration: 0.1, isReady: true);
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		StartTtl = Ttl;
		OuterStartWidth = OuterBeamSprite.Scale.Y;
		InnerStartWidth = InnerBeamSprite.Scale.Y;

		DamageCd.Ready += () =>
		{
			DoDamage(DamageCd.Duration);
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Ttl <= 0)
		{
			Shaker.IsAlive = false;
			
			var dummy = Particles.Drop();
			Particles.Emitting = false;
			dummy.Destruct(Particles.Lifetime * 3);
			
			QueueFree();
		}
		
		var ttlFactor = Ttl / StartTtl;
		var sizeFactor = SizeCurve.Sample(ttlFactor);
		
		Ttl -= delta;
		Ang += 1800 * delta;
		Ang %= 360;

		InnerSpawnSprite.Rotation += Mathf.DegToRad(360 * delta);
		OuterSpawnSprite.Rotation -= Mathf.DegToRad(360 * delta);
		OuterBeamSprite.Scale = OuterBeamSprite.Scale with { Y = OuterStartWidth * sizeFactor + OuterStartWidth * Mathf.Sin(Mathf.DegToRad(Ang)) * 0.07 };
		InnerBeamSprite.Scale = InnerBeamSprite.Scale with { Y = InnerStartWidth * sizeFactor + InnerStartWidth * Mathf.Sin(Mathf.DegToRad(Ang)) * 0.07 };

		DamageCd.Update(delta);
	}
	
	private void DoDamage(double delta)
	{
		Shaker.Strength = 10 * Mathf.Max(0, 1 - Source.DistanceTo(this) / ShakeDist); 
		
		var outerDamage = new Damage(Bullet.AuthorEnum.PLAYER, new Color(1, 0, 0), Dps * delta * 0.5 * Source.UniversalDamageMultiplier, Source);
		var innerDamage = new Damage(Bullet.AuthorEnum.PLAYER, new Color(1, 0, 0), Dps * delta * 2 * Source.UniversalDamageMultiplier, Source);
		
		var outerOthers = OuterHitArea.GetOverlappingAreas();
		var innerOthers = InnerHitArea.GetOverlappingAreas();
		
		foreach (var area in outerOthers)
		{
			if(area.GetParent() is not Enemy body) continue;
			var distFactor = Mathf.Max(0, 1 - (body.Position - Source.Position).Length() / 2000);
			body.Position += this.Right() * distFactor * PushVel * Source.UniversalDamageMultiplier * 0.5 * delta;
			body.TakeDamage(outerDamage);
		}
		
		foreach (var area in innerOthers)
		{
			if(area.GetParent() is not Enemy body) continue;
			var distFactor = Mathf.Max(0, 1 - (body.Position - Source.Position).Length() / 2000);
			body.Position += this.Right() * distFactor * PushVel * Source.UniversalDamageMultiplier * delta;
			body.TakeDamage(innerDamage);
		}
	}
}