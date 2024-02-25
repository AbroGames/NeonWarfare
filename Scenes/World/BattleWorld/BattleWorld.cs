using System.Collections.Generic;
using AbroDraft.Scenes.World.BattleWorld.Wave;
using AbroDraft.Scripts.EventBus;
using Godot;
using KludgeBox;

namespace AbroDraft.Scenes.World.BattleWorld;

public partial class BattleWorld : Node2D
{
	[Export] [NotNull] public Floor Floor { get; set; }
	
	public Screen.BattleHud.BattleHud BattleHud { get; set; }
	public Entities.Character.Player.Player Player;
	public EnemyWave EnemyWave { get; set; } = new();
	public readonly ISet<Entities.Character.Enemy.Enemy> Enemies = new HashSet<Entities.Character.Enemy.Enemy>();
	
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