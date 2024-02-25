using AbroDraft.Scripts.EventBus;
using Godot;
using KludgeBox;

namespace AbroDraft.Scenes.World.Entities.Character.Enemy;

public partial class Enemy : Character
{
	
	[Export] [NotNull] public RayCast2D RayCast { get; private set; }
	
	public int BaseXp { get; set; } = 1;
	public double Damage { get; set; } = 1000;
	public bool IsBoss { get; set; } = false;
	public Character Target { get; set; }
	public bool IsAttractor { get; set;}

	public override void _Ready()
	{
		base._Ready();
		EventBus.Publish(new EnemyReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		EventBus.Publish(new EnemyProcessEvent(this, delta));
	}
	
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		EventBus.Publish(new EnemyPhysicsProcessEvent(this, delta));
	}
	
}