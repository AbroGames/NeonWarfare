using System.Collections.Generic;
using Godot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.Utils.Camera.Shifts;

namespace NeonWarfare.Scripts.Utils.Camera;

public partial class Camera : Camera2D
{
	
	public Node2D TargetNode;
	
	public Vector2 TargetPosition = Vector2.Zero; // Position where camera wants to be
	public Vector2 ActualPosition = Vector2.Zero; // Position where camera is currently in
	public Vector2 PositionShift = Vector2.Zero; // Additional shift to ActualPosition
	public Vector2 HardPositionShift = Vector2.Zero; // Additional shift to ActualPosition that will not be smoothed. Usable for shake
	
	public double SmoothingBase = 0.1;
	public double SmoothingPower = 1.5; // The power to which the SmoothingBase value will be raised

	public float MinSpeed = 10; // Minimum possible camera speed in px/sec
	
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
		ActualPosition = Position;
		TargetPosition = Position;
	}

	public override void _Process(double delta)
	{
		MoveCamera(delta);
		UpdateShifts(delta);
	}
	
	public override void _Input(InputEvent @event) 
	{
		if (@event.IsActionPressed(Keys.WheelUp) || @event.IsActionPressed(Keys.WheelDown))
		{
			OnMouseWheel(@event);
		}
	}

	public Punch Punch(Vector2 rotation, double strength, double movementSpeed = 3000)
	{
		var punch = new Punch(rotation, strength, movementSpeed);
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
	
	private void MoveCamera(double delta)
	{
		if (TargetNode is null) return;
        
		TargetPosition = TargetNode.Position;
		var availableMovement = (TargetPosition + PositionShift) - ActualPosition;
		var actualMovement = availableMovement * (float) Mathf.Pow(SmoothingBase, SmoothingPower);
		var actualMovementLength = actualMovement.Length();
		actualMovement = availableMovement.Normalized() * (float)Mathf.Min(availableMovement.Length(), Mathf.Max(actualMovementLength, MinSpeed * delta));
			
		ActualPosition += actualMovement;
		Position = ActualPosition + HardPositionShift + AdditionalShift;
	}

	private void UpdateShifts(double delta)
	{
		foreach (var punch in Shifts)
		{
			punch.Update(delta);
		}

		Shifts.RemoveAll(s => !s.IsAlive);
	}
	
	private void OnMouseWheel(InputEvent @event)
	{
		if (@event.IsActionPressed(Keys.WheelUp))
		{
			Zoom *= 1.1f;
		}
		if (@event.IsActionPressed(Keys.WheelDown))
		{
			Zoom *= 1f / 1.1f;
		}
	}
}
