using System.Collections.Generic;
using Godot;
using KludgeBox;

namespace AbroDraft.World;

public partial class BattleWorld : Node2D
{
	[Export] [NotNull] public Floor Floor { get; set; }
	
	public BattleHud BattleHud { get; set; }
	public Player Player;
	public EnemyWave EnemyWave { get; set; } = new();
	public readonly ISet<Enemy> Enemies = new HashSet<Enemy>();
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		EventBus.Publish(new BattleWorldReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		EventBus.Publish(new BattleWorldProcessEvent(this, delta));
		EventBus.Publish(new BattleWorldDeferredProcessEvent(this, delta));
	}

	public override void _PhysicsProcess(double delta)
	{
		EventBus.Publish(new BattleWorldPhysicsProcessEvent(this, delta));
		EventBus.Publish(new BattleWorldDeferredPhysicsProcessEvent(this, delta));
	}
}