using System;
using Game.Content;
using Godot;
using KludgeBox;
using KludgeBox.Scheduling;
using MicroSurvivors;

public partial class Character : CharacterBody2D
{

	[Export] private double _movementSpeed = 250; // in pixels/sec
	[Export] private double _rotationSpeed = 300; // in degree/sec
	[Export] private double _attackSpeed = 3; // attack/sec

	[Export] private PackedScene _bulletBlueprint;

	public int MaxHp { get; private set; } = 10000;
	public int Hp = 10000;

	private double _secToNextAttack = 0;
	private Vector2 _spritePos;
	public double HitFlash = 0; // needs to be public since all hit logic is in Bullet class

	private Sprite2D Sprite => GetNode("Sprite2D") as Sprite2D;
	private Sprite2D ShieldSprite => GetNode("ShieldSprite") as Sprite2D;

	private PlayerCamera _camera;

	private Cooldown _secondaryCd = new(0.1);
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_spritePos = GlobalPosition;
		_camera = GetParent().GetChild<PlayerCamera>();
		_secondaryCd.Ready += AttackSecondary;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		RotateToMouse(delta);
		AttackPrimary(delta);
		MoveSprite(delta);

		_secondaryCd.Update(delta);
		
		// flash effect on hit processing
		HitFlash -= 0.02;
		HitFlash = Mathf.Max(HitFlash, 0);

		ShieldSprite.Modulate = Modulate with { A = (float)HitFlash };
		var shader = Sprite.Material as ShaderMaterial;
		shader.SetShaderParameter("colorMaskFactor", HitFlash);
		
		// Camera shift processing
		if (Input.IsActionPressed(Keys.CameraShift))
		{
			var maxShift = GetGlobalMousePosition() - GlobalPosition;
			var zoomFactor = (_camera.Zoom.X + _camera.Zoom.Y) / 2;
			_camera.PositionShift = maxShift * 0.7 * zoomFactor;
		}
		else
		{
			_camera.PositionShift = Vec();
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		var movementInput = GetInput();
		// Переместить и првоерить физику
		MoveAndCollide(movementInput * _movementSpeed * delta);
	}
	
	private Vector2 GetInput()
	{
		return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
	}

	private void RotateToMouse(double delta)
	{
		//Куда хотим повернуться
		double targetAngle = GetAngleToMouse();
		//На какой угол надо повернуться (знак указывает направление)
		double deltaAngleToTargetAngel = Mathf.AngleDifference(Rotation - Mathf.Pi / 2, targetAngle);
		//Только направление (-1, 0, 1)
		double directionToTargetAngel = Mathf.Sign(deltaAngleToTargetAngel);
		//Максимальная скорость поворота (за секунду)
		double rotationSpeedRad = Mathf.DegToRad(_rotationSpeed);
		//Максимальная скорость поворота (за прошедшее время)
		rotationSpeedRad *= delta;
		//Если надо повернуться на угол меньший максимальной скорости, то обрезаем скорость, чтобы повернуться ровно в цель
		rotationSpeedRad = Math.Min(rotationSpeedRad, Math.Abs(deltaAngleToTargetAngel));
		//Добавляем к скорости поворота направление, чтобы поворачивать в сторону цели
		rotationSpeedRad *= directionToTargetAngel;
		//Поворачиваемся на угол
		Rotation += rotationSpeedRad;
	}
	
	private double GetAngleToMouse()
	{
		// Получаем текущую позицию мыши
		var mousePos = GetGlobalMousePosition();
		// Вычисляем вектор направления от объекта к мыши
		var mouseDir = GlobalPosition.DirectionTo(mousePos);
		// Вычисляем направление от объекта к мыши
		return mouseDir.Angle();
	}
	
	private void AttackPrimary(double delta)
	{
		_secToNextAttack -= delta;
		if (!Input.IsActionPressed(Keys.AttackPrimary)) return;
		if (_secToNextAttack > 0) return;

		_secToNextAttack = 1.0 / _attackSpeed;
		
		// Создание снаряда
		Bullet bullet = _bulletBlueprint.Instantiate() as Bullet;
		// Установка начальной позиции снаряда
		bullet.GlobalPosition = GlobalPosition;
		// Установка направления движения снаряда
		bullet.Rotation = Rotation;
		bullet.Author = Bullet.AuthorEnum.PLAYER;
		bullet.Speed *= 3;
		bullet.GetNode<Sprite2D>("Sprite2D").Scale *= 2;
		Audio2D.PlaySoundAt(Sfx.SmallLaserShot, Position, 1f);
		GetParent().AddChild(bullet);
	}
	
	private void AttackSecondary()
	{
		if (!Input.IsActionPressed(Keys.AttackSecondary)) return;
		
		// Создание снаряда
		Bullet bullet = _bulletBlueprint.Instantiate() as Bullet;
		// Установка начальной позиции снаряда
		bullet.GlobalPosition = GlobalPosition;
		// Установка направления движения снаряда
		var spread = Mathf.DegToRad(6);
		bullet.Rotation = Rotation + Rand.Range(-spread, spread);
		bullet.Author = Bullet.AuthorEnum.PLAYER;
		bullet.Speed *= 2;
		bullet.RemainingDistance /= 2;
		bullet.RemainingDamage = 50;
		var modulate = bullet.GetNode<Sprite2D>("Sprite2D").Modulate;
		bullet.GetNode<Sprite2D>("Sprite2D").SelfModulate = modulate.Darkened(0.2f);
		Audio2D.PlaySoundAt(Sfx.SmallLaserShot, Position, 0.5f);
		GetParent().AddChild(bullet);
	}

	private void MoveSprite(double delta)
	{
		var requiredMovement = GlobalPosition - _spritePos;

		var stepFactor = delta / (1.0 / 60);
		var smoothingFactor = 0.5;
		_spritePos += requiredMovement * stepFactor * smoothingFactor;
		
		Sprite.GlobalPosition = _spritePos;
		ShieldSprite.GlobalPosition = _spritePos;
	}
}