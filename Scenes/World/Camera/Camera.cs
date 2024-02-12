using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

public partial class Camera : Camera2D
{
	
	public Node2D TargetNode;
	
	public Vector2 TargetPosition = Vector2.Zero; // Position where camera wants to be
	public Vector2 ActualPosition = Vector2.Zero; // Position where camera is currently in
	public Vector2 PositionShift = Vector2.Zero; // Additional shift to ActualPosition
	public Vector2 HardPositionShift = Vector2.Zero; // Additional shift to ActualPosition that will not be smoothed. Usable for shake
	
	public double SmoothingBase = 0.1;
	public double SmoothingPower = 1.5; // The power to which the SmoothingBase value will be raised
	
	public List<Punch> Punches = new();
	public List<Shake> Shakes = new();

	public Vector2 PunchShift
	{
		get
		{
			var shift = Vec();
			foreach (Punch punch in Punches)
			{
				shift += punch.Shift;
			}

			return shift;
		}
	}

	public Vector2 ShakeShift
	{
		get
		{
			var shift = Vec();
			foreach (Shake shake in Shakes)
			{
				shift += shake.Shift;
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
		Punches.Add(punch);
		return punch;
	}

	public Shake Shake(double strength, double time, bool deceising = true)
	{
		var shake = new Shake(strength, time, deceising);
		Shakes.Add(shake);
		return shake;
	}
}

public class Punch(Vector2 dir, double strength, double movementSpeed = 3000)
{
	public Vector2 Direction { get; private set; } = dir.Normalized();
	public double Strength { get; private set; } = strength;
	public double InitialStrength { get; private set; } = strength;

	public Vector2 Shift => Direction * Strength;
	public bool IsAlive => Strength > Mathf.Epsilon;
	public void Update(double delta)
	{
		Strength = Mathf.Max(0, Strength - movementSpeed * delta);
	}
}

public class Shake(double strength, double time, bool deceising = true)
{
	public double Time { get; private set; } = time;
	public double InitialTime { get; private set; } = time;

	public double Strength => InitialStrength * (Time / InitialTime);
	public double InitialStrength { get; private set; } = strength;

	public Vector2 Shift => Rand.InsideUnitCircle * (deceising ? Strength : InitialStrength);
	public bool IsAlive => Time > Mathf.Epsilon;
		
	public void Update(double delta)
	{
		Time = Mathf.Max(0, Time - delta);
	}
}