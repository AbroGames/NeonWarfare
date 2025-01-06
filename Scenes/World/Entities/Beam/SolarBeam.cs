using Godot;
using KludgeBox;
using KludgeBox.Scheduling;

namespace NeonWarfare;

public partial class SolarBeam : Node2D
{
	[Export] [NotNull] public Area2D OuterHitArea { get; private set; }
	[Export] [NotNull] public Sprite2D OuterSpawnSprite { get; set; }
	[Export] [NotNull] public Sprite2D OuterBeamSprite { get; set; }
	
	[Export] [NotNull] public Area2D InnerHitArea { get; private set; }
	[Export] [NotNull] public Sprite2D InnerSpawnSprite { get; set; }
	[Export] [NotNull] public Sprite2D InnerBeamSprite { get; set; }
	[Export] [NotNull] public CpuParticles2D Particles { get; set; }
	
	public Player Source { get; set; }
	public double Dps { get; set; } = 3000;
	public double RotationSpeedFactor = 0.075;
	public double PushVel { get; set; } = 500;
	public double Ttl = 7;
	
	private double _innerStartWidth;
	private double _outerStartWidth;
	private double _ang;
	private float _startGlow;
	private double _interpolationFactor = (240.0 / 60) * 60;
	private Cooldown _damageCd = new(duration: 0.1, isReady: true);
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		_outerStartWidth = OuterBeamSprite.Scale.Y;
		_innerStartWidth = InnerBeamSprite.Scale.Y;
		Source.RotationSpeed *= RotationSpeedFactor;
		_damageCd.Ready += () =>
		{
			DoDamage(_damageCd.Duration);
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
		_ang += 1800 * delta;
		_ang %= 360;
		var shrinkFactor = Mathf.Min(1, Ttl * 4);

		InnerSpawnSprite.Rotation += (float) Mathf.DegToRad(360 * delta);
		OuterSpawnSprite.Rotation -= (float) Mathf.DegToRad(360 * delta);
		OuterBeamSprite.Scale = OuterBeamSprite.Scale with { Y = (float) ((_outerStartWidth + _outerStartWidth * Mathf.Sin(Mathf.DegToRad(_ang)) * 0.07) * shrinkFactor) };
		InnerBeamSprite.Scale = InnerBeamSprite.Scale with { Y = (float) ((_innerStartWidth + _innerStartWidth * Mathf.Sin(Mathf.DegToRad(_ang)) * 0.07) * shrinkFactor) };
		
		_damageCd.Update(delta);
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
			body.Position += this.Right() * (float) (distFactor * PushVel * Source.UniversalDamageMultiplier * 0.5 * delta);
			body.TakeDamage(outerDamage);
		}
		
		foreach (var area in innerOthers)
		{
			if(area.GetParent() is not Enemy body) continue;
			var distFactor = Mathf.Max(0, 1 - (body.Position - Source.Position).Length() / 2000);
			body.Position += this.Right() * (float) (distFactor * PushVel * Source.UniversalDamageMultiplier * delta);
			body.TakeDamage(innerDamage);
		}
	}
}