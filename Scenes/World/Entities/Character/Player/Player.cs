using System;
using Godot;
using KludgeBox;
using KludgeBox.Networking;
using KludgeBox.Scheduling;

namespace NeonWarfare;

public partial class Player : Character
{
	[Export] [NotNull] public Sprite2D ShieldSprite { get; private set; }
	
	public double PrimaryDamage { get; set; } = 1000;
	public double PrimaryDistance { get; set; } = 2000;
	public double UniversalDamageMultiplier { get; set; } = 1;
	
	public double SecondaryDamage { get; set; } = 5;
	public double SecondaryDistance { get; set; } = 1000;

	public Camera Camera;
	
	public Cooldown SecondaryCd { get; set; } = new(0.1, CooldownMode.Cyclic, true);

	public Cooldown BasicAbilityCd { get; set; } = new(6, CooldownMode.Single, true);
	public Cooldown AdvancedAbilityCd { get; set; } = new(50, CooldownMode.Single, true);
	
	//TODO serverInfo (mb ServerPlayer)?
	public Vector2 CurrentMovementVector { get; set; }
	public double CurrentMovementSpeed { get; set; }
	
	public override void _Ready()
	{
		base._Ready();
		if (Netplay.Mode == Netplay.Netmode.Server) return;
		
		Camera = GetParent().GetChild<Camera>();
		Sprite.Modulate = ClientRoot.Instance.PlayerSettings.PlayerColor;
        
		SecondaryCd.Ready += () =>
		{
			if (!Input.IsActionPressed(Keys.AttackSecondary)) return;
			Netplay.SendToServer(new ClientPlayerSecondaryAttackPacket(Position.X, Position.Y, Rotation));
			//new PlayerAttackService().OnServerPlayerSecondaryAttackPacket(new ServerPlayerSecondaryAttackPacket(new Random().NextInt64(), Position.X, Position.Y, Rotation, 2000));
		};
	}

	public override void Shoot()
	{
		base.Shoot();
		//if (!Input.IsActionPressed(Keys.AttackPrimary)) return;
		
		Netplay.SendToServer(new ClientPlayerPrimaryAttackPacket(Position.X, Position.Y, Rotation));
		//TODO костыль для теста снаряда локально. Закомментить передачу по сети, раскомментить строку ниже.
		//new PlayerAttackService().OnServerPlayerPrimaryAttackPacket(new ServerPlayerPrimaryAttackPacket(new Random().NextInt64(), Position.X, Position.Y, Rotation, 2000));
	}

	public override void Die()
	{
		base.Die();
		
		ClientRoot.Instance.CreateMainMenu();
		Audio2D.StopMusic();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		
		Hp += RegenHpSpeed * delta;
		Hp = Mathf.Min(Hp, MaxHp);

		//PrimaryCd.Update(delta);
		SecondaryCd.Update(delta);
		BasicAbilityCd.Update(delta);
		AdvancedAbilityCd.Update(delta);

		ShieldSprite.Modulate = Modulate with { A = (float)HitFlash };

		if (Netplay.IsClient)
		{
			// Camera shift processing
			if (Input.IsActionPressed(Keys.CameraShift))
			{
				var maxShift = GetGlobalMousePosition() - GlobalPosition;
				var zoomFactor = (Camera.Zoom.X + Camera.Zoom.Y) / 2;
				Camera.PositionShift = maxShift * 0.7 * zoomFactor;
			}
			else
			{
				Camera.PositionShift = Vec();
			}
		}
	}

	//TODO вынести в отдельные компоненты (и вообще отдельный абстрактный класс для оружия, для визуала и т.п.)
	public override void _Input(InputEvent @event) 
	{
		base._Input(@event);
		
		if (@event.IsActionPressed(Keys.AbilityBasic))
		{
			if (BasicAbilityCd.Use())
			{
				UseBasicSkill();
			}
		}

		if (@event.IsActionPressed(Keys.AbilityAdvanced))
		{
			if (AdvancedAbilityCd.Use())
			{
				UseAdvancedSkill();
			}
		}
	}

	private void UseBasicSkill()
	{
		var node = ClientRoot.Instance.PackedScenes.Common.World.Beam.Instantiate();
		var beam = node as Beam;
		var shaker = Camera.ShakeManually();
		beam.Shaker = shaker;
        
		beam.Rotation = Rotation - Mathf.Pi / 2;
		GetParent().AddChild(beam);
		beam.Position = Position;
		beam.Source = this;
		//beam.Modulate = Sprite.Modulate;
		beam.Dps *= UniversalDamageMultiplier;
		Position -= this.Up() * 250;
        
		Audio2D.PlaySoundOn(Sfx.HornImpact3, this);
		Audio2D.PlaySoundOn(Sfx.DeepImpact, this);
	}
	
	private void UseAdvancedSkill()
	{
		var node = ClientRoot.Instance.PackedScenes.Common.World.SolarBeam.Instantiate();
		var beam = node as SolarBeam;
		beam.Rotation = -Mathf.Pi / 2;
		beam.Source = this;
		//beam.Modulate = Sprite.Modulate;
		beam.Dps *= UniversalDamageMultiplier;
		Camera.Shake(10, beam.Ttl, false);
		AddChild(beam);
        
		Audio2D.PlaySoundOn(Sfx.LaserBeam, this);
		Audio2D.PlaySoundOn(Sfx.LaserBig, this);
		Audio2D.PlaySoundOn(Sfx.Beam, this);
	}
}