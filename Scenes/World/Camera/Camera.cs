using System.Collections.Generic;
using AbroDraft.Scenes.World.Camera.Shifts;
using AbroDraft.Scripts.EventBus;
using Godot;

namespace AbroDraft.Scenes.World.Camera;

public partial class Camera : Camera2D
{
	
	public Node2D TargetNode;
	
	public Vector2 TargetPosition = Vector2.Zero; // Position where camera wants to be
	public Vector2 ActualPosition = Vector2.Zero; // Position where camera is currently in
	public Vector2 PositionShift = Vector2.Zero; // Additional shift to ActualPosition
	public Vector2 HardPositionShift = Vector2.Zero; // Additional shift to ActualPosition that will not be smoothed. Usable for shake
	
	public double SmoothingBase = 0.1;
	public double SmoothingPower = 1.5; // The power to which the SmoothingBase value will be raised
	
	public List<IShiftProvider> Shifts = new();

	public Vector2 AdditionalShift
	{
		get
		{
			var shift = Vec();
			foreach (IShiftProvider punch in Shifts)
			{
				shift += punch.Shift;
			}

			return shift;
		}
	}

	public override void _Ready()
	{
		EventBus.Publish(new CameraReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		EventBus.Publish(new CameraProcessEvent(this, delta));
		EventBus.Publish(new CameraDeferredProcessEvent(this, delta));
	}

	public Punch Punch(Vector2 dir, double strength, double movementSpeed = 3000)
	{
		var punch = new Punch(dir, strength, movementSpeed);
		Shifts.Add(punch);
		return punch;
	}

	public Shake Shake(double strength, double time, bool deceising = true)
	{
		var shake = new Shake(strength, time, deceising);
		Shifts.Add(shake);
		return shake;
	}

	public ManualShake ShakeManually()
	{
		var shake = new ManualShake();
		Shifts.Add(shake);
		return shake;
	}
}