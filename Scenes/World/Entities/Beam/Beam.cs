using Godot;
using System;
using KludgeBox;
using Scenes.World;

public partial class Beam : Node2D
{
	public Player Source { get; set; }
	public double DPS { get; set; } = 1000;
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
		env.GlowStrength = 2;
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
		_ang += 3600 * delta;
		_ang %= 360 * delta;

		SpawnSprite.Rotation += Mathf.DegToRad(360 * delta);
		BeamSprite.Scale = BeamSprite.Scale with { Y = _startWidth + _startWidth * Mathf.Sin(Mathf.DegToRad(_ang)) * 0.1 };
		
		var damage = new Damage(Bullet.AuthorEnum.PLAYER, new Color(1, 0, 0), DPS * delta, Source);
		var others = HitArea.GetOverlappingAreas();
		Log.Info(others.Count);
		foreach (var area in others)
		{
			if(area.GetParent() is not Enemy body) continue;
			body.TakeDamage(damage);
		}
	}
}
