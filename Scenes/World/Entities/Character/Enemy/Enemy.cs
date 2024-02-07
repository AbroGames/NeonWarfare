using Godot;
using System;
using Game.Content;

public partial class Enemy : Character
{
	
	[Export] [NotNull] public RayCast2D RayCast { get; private set; }
	
	public int BaseXp { get; set; } = 1;
	public double Damage = 1000;
	public bool IsBoss = false;
	public Character Target;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		RegenHpSpeed = 0;
		MaxHp = Hp;
		Died += () =>
		{
			(GetParent() as BattleWorld)?.Enemies.Remove(this);
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Root.Instance.EventBus.Publish(new EnemyProcessEvent(this, delta));
		base._Process(delta); //TODO del? Или мы хотим кидать ивенты ещё и с Character? Вообще наследование не очень хорошо. Лучше переиспользовать код обращаясь к CharacterService из EnemyService
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Root.Instance.EventBus.Publish(new EnemyPhysicsProcessEvent(this, delta));
	}
	
}
