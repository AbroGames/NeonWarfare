using Godot;
using System;
using System.Collections.Generic;
using Game.Content;
using KludgeBox;

public partial class BattleWorld : Node2D
{
	public BattleHud BattleHud { get; set; }
	public Player Player;
	public EnemyWave EnemyWave { get; set; } = new();
	public readonly ISet<Enemy> Enemies = new HashSet<Enemy>();
	
	public override void _Ready()
	{
		Root.Instance.EventBus.Publish(new BattleWorldReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		Root.Instance.EventBus.Publish(new BattleWorldProcessEvent(this, delta));
	}
}
