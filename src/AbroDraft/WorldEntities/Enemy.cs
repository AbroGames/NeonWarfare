using Godot;
using System;
using Game.Content;

public partial class Enemy : CharacterBody2D
{
	
	public double HitFlash = 0; // needs to be public since all hit logic is in Bullet class
	[Export] private double _movementSpeed = 200; // in pixels/sec
	[Export] public int Hp = 250;
	[Export] private double _attackSpeed = 1; // attack/sec

	private RayCast2D RayCast => GetNode("RayCast") as RayCast2D;

	[Export] private PackedScene _bulletBlueprint;
	
	public Character Target;
	private double _secToNextAttack = 0;
	
	private Sprite2D Sprite => GetNode("Sprite2D") as Sprite2D;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Rotation = GetAngleToTarget() + Mathf.Pi / 2;
		
		// flash effect on hit processing
		HitFlash -= 0.02;
		HitFlash = Mathf.Max(HitFlash, 0);
		var shader = Sprite.Material as ShaderMaterial;
		shader.SetShaderParameter("colorMaskFactor", HitFlash);
		
		if (CanShoot())
		{
			Attack(delta);
		}
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

	private bool CanShoot()
	{
		var collider = RayCast.GetCollider();
		if (collider is Character chara)
		{
			return true;
		}

		return false;
	}

	private void Attack(double delta)
	{
		_secToNextAttack -= delta;
		if (_secToNextAttack > 0) return;

		_secToNextAttack = 1.0 / _attackSpeed;
		
		// Создание снаряда
		Bullet bullet = _bulletBlueprint.Instantiate() as Bullet;
		// Установка начальной позиции снаряда
		bullet.GlobalPosition = GlobalPosition;
		// Установка направления движения снаряда
		bullet.Rotation = Rotation;
		bullet.Author = Bullet.AuthorEnum.ENEMY;
		
		Audio2D.PlaySoundAt(Sfx.SmallLaserShot, Position, 0.7f);
		GetParent().AddChild(bullet);
	}
}
