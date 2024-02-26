using Godot;
using KludgeBox;
using KludgeBox.Scheduling;

namespace NeoVector.World;

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
	
	private double _ttl = 2;
	private double _startTtl;
	private double _innerStartWidth;
	private double _outerStartWidth;
	private double _ang;
	private float _startGlow;
	private double _shakeDist = 1500;
	private Cooldown _damageCd = new(duration: 0.1, isReady: true);
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		_startTtl = _ttl;
		_outerStartWidth = OuterBeamSprite.Scale.Y;
		_innerStartWidth = InnerBeamSprite.Scale.Y;

		_damageCd.Ready += () =>
		{
			DoDamage(_damageCd.Duration);
		};
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_ttl <= 0)
		{
			Shaker.IsAlive = false;
			
			var dummy = Particles.Drop();
			Particles.Emitting = false;
			dummy.Destruct(Particles.Lifetime * 3);
			
			QueueFree();
		}
		
		var ttlFactor = _ttl / _startTtl;
		var sizeFactor = SizeCurve.Sample(ttlFactor);
		
		_ttl -= delta;
		_ang += 1800 * delta;
		_ang %= 360;

		InnerSpawnSprite.Rotation += Mathf.DegToRad(360 * delta);
		OuterSpawnSprite.Rotation -= Mathf.DegToRad(360 * delta);
		OuterBeamSprite.Scale = OuterBeamSprite.Scale with { Y = _outerStartWidth * sizeFactor + _outerStartWidth * Mathf.Sin(Mathf.DegToRad(_ang)) * 0.07 };
		InnerBeamSprite.Scale = InnerBeamSprite.Scale with { Y = _innerStartWidth * sizeFactor + _innerStartWidth * Mathf.Sin(Mathf.DegToRad(_ang)) * 0.07 };

		_damageCd.Update(delta);
	}

	private void DoDamage(double delta)
	{
		double interpolationFactor = (240.0 / 60) * 60;

		Shaker.Strength = 10 * Mathf.Max(0, 1 - Source.DistanceTo(this) / _shakeDist); 
		
		
		
		var outerDamage = new Damage(Bullet.AuthorEnum.PLAYER, new Color(1, 0, 0), Dps * delta * 0.5, Source);
		var innerDamage = new Damage(Bullet.AuthorEnum.PLAYER, new Color(1, 0, 0), Dps * delta * 2, Source);
		
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