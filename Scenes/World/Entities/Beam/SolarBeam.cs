using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Scheduling;

namespace NeoVector;

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
		EventBus.Publish(new SolarBeamSpawnedEvent(this));
	}

	public override void _PhysicsProcess(double delta)
	{
		EventBus.Publish(new SolarBeamPhysicsProcessEvent(this, delta));
	}
}