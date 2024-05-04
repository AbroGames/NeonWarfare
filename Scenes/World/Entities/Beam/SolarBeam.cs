using Godot;
using KludgeBox;
using KludgeBox.Scheduling;

namespace NeonWarfare;

//TODO в общего родителя с обычным Beam
public partial class SolarBeam : Node2D
{
	public Player Source { get; set; }
	public double Dps { get; set; } = 3000;
	public double RotationSpeedFactor = 0.075;
	public double PushVel { get; set; } = 500;
	[Export] [NotNull] public Area2D OuterHitArea { get; private set; }
	[Export] [NotNull] public Sprite2D OuterSpawnSprite { get; set; }
	[Export] [NotNull] public Sprite2D OuterBeamSprite { get; set; }
	
	[Export] [NotNull] public Area2D InnerHitArea { get; private set; }
	[Export] [NotNull] public Sprite2D InnerSpawnSprite { get; set; }
	[Export] [NotNull] public Sprite2D InnerBeamSprite { get; set; }
	[Export] [NotNull] public CpuParticles2D Particles { get; set; }

	public double Ttl = 7;
	public double InnerStartWidth;
	public double OuterStartWidth;
	public double Ang;
	public float StartGlow;
	public double InterpolationFactor = (240.0 / 60) * 60;
	public Cooldown DamageCd = new(duration: 0.1, isReady: true);

	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		OuterStartWidth = OuterBeamSprite.Scale.Y;
		InnerStartWidth = InnerBeamSprite.Scale.Y;
		Source.RotationSpeed *= RotationSpeedFactor;
		DamageCd.Ready += () =>
		{
			DoDamage(DamageCd.Duration);
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Ttl <= 0)
		{
			var dummy = Particles.Drop();
			Particles.Emitting = false;
			dummy.Destruct(Particles.Lifetime * 3);
			Source.RotationSpeed /= RotationSpeedFactor;
			QueueFree();
		}
		
		
		Ttl -= delta;
		Ang += 1800 * delta;
		Ang %= 360;
		var shrinkFactor = Mathf.Min(1, Ttl * 4);

		InnerSpawnSprite.Rotation += Mathf.DegToRad(360 * delta);
		OuterSpawnSprite.Rotation -= Mathf.DegToRad(360 * delta);
		OuterBeamSprite.Scale = OuterBeamSprite.Scale with { Y = (OuterStartWidth + OuterStartWidth * Mathf.Sin(Mathf.DegToRad(Ang)) * 0.07) * shrinkFactor };
		InnerBeamSprite.Scale = InnerBeamSprite.Scale with { Y = (InnerStartWidth + InnerStartWidth * Mathf.Sin(Mathf.DegToRad(Ang)) * 0.07) * shrinkFactor };
		
		DamageCd.Update(delta);
	}
	
	private void DoDamage(double delta)
	{
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