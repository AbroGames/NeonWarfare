using Godot;
using System;

public partial class Enemy : CharacterBody2D
{
	
	[Export] private double _movementSpeed = 200; // in pixels/sec
	[Export] public int Hp = 250;

	public Character Target;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Rotation = GetAngleToTarget() + Mathf.Pi / 2;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		var directionToMove = Vector2.FromAngle(Rotation - Mathf.Pi / 2);
		// Переместить и првоерить физику
		MoveAndCollide(directionToMove * _movementSpeed * delta);
	}
	
	private double GetAngleToTarget()
	{
		// Получаем текущую позицию мыши
		var targetPos = Target.GlobalPosition;
		// Вычисляем вектор направления от объекта к мыши
		var targetDir = GlobalPosition.DirectionTo(targetPos);
		// Вычисляем направление от объекта к мыши
		return targetDir.Angle();
	}
}
