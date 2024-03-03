using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Events.Global.World;

namespace NeoVector;

public partial class BattleWorld : World
{
	public BattleHud BattleHud { get; set; }
	public EnemyWave EnemyWave { get; set; } = new();
	public readonly ISet<Enemy> Enemies = new HashSet<Enemy>();
	
	public override void _Ready()
	{
		base._Ready();
		EventBus.Publish(new BattleWorldReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		EventBus.Publish(new BattleWorldProcessEvent(this, delta));
		EventBus.Publish(new BattleWorldDeferredProcessEvent(this, delta));
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		EventBus.Publish(new BattleWorldPhysicsProcessEvent(this, delta));
		EventBus.Publish(new BattleWorldDeferredPhysicsProcessEvent(this, delta));
	}
}