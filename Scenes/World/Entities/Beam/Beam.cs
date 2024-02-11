using Godot;
using System;
using KludgeBox;
using Scenes.World;

public partial class Beam : Node2D
{
	public Player Source { get; set; }
	public double Dps { get; set; } = 3000;
	[Export] [NotNull] public Area2D HitArea { get; private set; }
	[Export] [NotNull] public Sprite2D SpawnSprite { get; set; }
	[Export] [NotNull] public Sprite2D BeamSprite { get; set; }
	
	private double _ttl = 7;
	private double _startWidth;
	private double _ang;
	private float _startGlow;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		_startWidth = BeamSprite.Scale.Y;
		var env = Root.Instance.Environment.Environment;
		_startGlow = env.GlowStrength;
		env.GlowStrength *= 1.1f;
	}

	public override void _Process(double delta)
	{
		if (_ttl <= 0)
		{
			var env = Root.Instance.Environment.Environment;
			env.GlowStrength = _startGlow;
			QueueFree();
		}
		_ttl -= delta;
		_ang += 1800 * delta;
		_ang %= 360;

		SpawnSprite.Rotation += Mathf.DegToRad(360 * delta);
		BeamSprite.Scale = BeamSprite.Scale with { Y = _startWidth + _startWidth * Mathf.Sin(Mathf.DegToRad(_ang)) * 0.03 };
		
		var damage = new Damage(Bullet.AuthorEnum.PLAYER, new Color(1, 0, 0), Dps * delta, Source);
		var others = HitArea.GetOverlappingAreas();
		Log.Info(others.Count);
		foreach (var area in others)
		{
			if(area.GetParent() is not Enemy body) continue;
			var distFactor = Mathf.Max(1, 1 - (body.Position - Source.Position).Length() / 1000);
			body.Position += Source.Up() * distFactor * 5;
			body.TakeDamage(damage);
		}
	}
}
