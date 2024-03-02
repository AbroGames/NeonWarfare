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
		EventBus.Publish(new BeamSpawnedEvent(this));
	}

	public override void _PhysicsProcess(double delta)
	{
		EventBus.Publish(new BeamPhysicsProcessEvent(this, delta));
	}
}