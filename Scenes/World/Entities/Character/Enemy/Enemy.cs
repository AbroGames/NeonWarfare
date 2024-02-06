using Godot;
using System;
using Game.Content;

public partial class Enemy : Character
{

	private RayCast2D RayCast => GetNode("RayCast") as RayCast2D;
	
	public Character Target;
	
	private Sprite2D Sprite => GetNode("Sprite2D") as Sprite2D;
	// Called when the node enters the scene tree for the first time.
	public override void Init()
	{
		_regenHpSpeed = 0;
		_movementSpeed = 200; // in pixels/sec
		Hp = 250;
		MaxHp = Hp;
		_movementSpeed *= 0.95;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void Update(double delta)
	{
		Rotation = GetAngleToTarget() + Mathf.Pi / 2;
		
		
		if (CanShoot())
		{
			Attack(delta);
		}
	}
	
	public override void PhysicsUpdate(double delta)
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
		if (collider is Player chara)
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
		Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
		// Установка начальной позиции снаряда
		bullet.GlobalPosition = GlobalPosition;
		// Установка направления движения снаряда
		bullet.Rotation = Rotation;
		bullet.Author = Bullet.AuthorEnum.ENEMY;
		
		Audio2D.PlaySoundAt(Sfx.SmallLaserShot, Position, 0.7f);
		GetParent().AddChild(bullet);
	}
}
