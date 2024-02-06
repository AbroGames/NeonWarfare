using Godot;
using System;
using Game.Content;
using KludgeBox;
using KludgeBox.Scheduling;
using MicroSurvivors;

public partial class Player : Character
{
	public static double RequiredXpLevelFactor { get; set; } = 1.5;
	public static int BasicRequiredXp { get; set; } = 10;
	public int Xp { get; protected set; }
	public int RequiredXp => (int)(BasicRequiredXp * Mathf.Pow(RequiredXpLevelFactor, Level));

	public int Level { get; protected set; } = 1;

	public double PrimaryDamage { get; protected set; } = 1000;
	public double SecondaryDamage { get; protected set; } = 5;
	
	
	private Sprite2D ShieldSprite => GetNode("ShieldSprite") as Sprite2D;

	private PlayerCamera _camera;

	private Cooldown _secondaryCd = new(0.1);
	// Called when the node enters the scene tree for the first time.
	public override void Init()
	{
		_camera = GetParent().GetChild<PlayerCamera>();
		_secondaryCd.Ready += AttackSecondary;

		_attackSpeed = 3;
		Died += () =>
		{
			var mainMenu = Root.Instance.PackedScenes.Main.MainMenu;
			Root.Instance.Game.MainSceneContainer.ChangeStoredNode(mainMenu.Instantiate());
		};
	}

	public void AddXp(int amount)
	{
		Xp += amount;
		if (Xp >= RequiredXp)
			LevelUp();
	}
	
	protected void LevelUp()
	{
		Xp -= RequiredXp;
		Level++;
		
		MaxHp *= 1.1;
		_regenHpSpeed *= 1.1;
		Hp = MaxHp;

		PrimaryDamage *= 1.1;
		SecondaryDamage *= 1.5;

		_movementSpeed *= 1.05;
		
		_attackSpeed *= 1.1;
		_secondaryCd.Duration /= 1.1;
		
		
		Audio2D.PlaySoundOn(Sfx.LevelUp, this, 1f);
		var lvlUpLabel =
			GD.Load<PackedScene>("res://Scenes/World/Entities/DamageLabel/FloatingLabel.tscn")
				.Instantiate() as FloatingLabel;
		
		lvlUpLabel.Configure($"Level up!\n({Level-1} -> {Level})", new Color(0, 1, 1), 1.3);
		lvlUpLabel.Position = Position - Vec(0, 100);
		GetParent().AddChild(lvlUpLabel);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void Update(double delta)
	{
		RotateToMouse(delta);
		AttackPrimary(delta);
		ShieldSprite.GlobalPosition = SmoothedPosition;

		Hp += _regenHpSpeed * delta;
		Hp = Math.Min(Hp, MaxHp);

		_secondaryCd.Update(delta);

		ShieldSprite.Modulate = Modulate with { A = (float)HitFlash };
		
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
	
	public override void PhysicsUpdate(double delta)
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
		Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
		// Установка начальной позиции снаряда
		bullet.GlobalPosition = GlobalPosition;
		// Установка направления движения снаряда
		bullet.Rotation = Rotation;
		bullet.Author = Bullet.AuthorEnum.PLAYER;
		bullet.Speed *= 3;
		bullet.RemainingDamage = PrimaryDamage;
		bullet.GetNode<Sprite2D>("Sprite2D").Scale *= 2;
		bullet.Source = this;
		Audio2D.PlaySoundAt(Sfx.SmallLaserShot, Position, 1f);
		GetParent().AddChild(bullet);
	}
	
	private void AttackSecondary()
	{
		if (!Input.IsActionPressed(Keys.AttackSecondary)) return;
		
		Audio2D.PlaySoundAt(Sfx.SmallLaserShot, Position, 0.5f);
		var bulletsCount = 5;
		var spread = Mathf.DegToRad(18);
		var speedSpread = 0.1;
		
		for (int i = 0; i < 5; i++)
		{
			// Создание снаряда
			Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
			// Установка начальной позиции снаряда
			bullet.GlobalPosition = GlobalPosition;
			// Установка направления движения снаряда
			bullet.Rotation = Rotation + Rand.Range(-spread, spread);
			bullet.Author = Bullet.AuthorEnum.PLAYER;
			bullet.Speed = bullet.Speed * 2 + Rand.Range(-bullet.Speed * speedSpread, bullet.Speed * speedSpread);
			bullet.RemainingDistance /= 2;
			bullet.RemainingDamage = SecondaryDamage;
			var modulate = bullet.GetNode<Sprite2D>("Sprite2D").Modulate;
			bullet.GetNode<Sprite2D>("Sprite2D").SelfModulate = modulate.Darkened(0.2f);
			bullet.Source = this;
			GetParent().AddChild(bullet);
		}
		
	}
}
