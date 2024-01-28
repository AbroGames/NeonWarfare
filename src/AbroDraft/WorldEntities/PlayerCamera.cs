using Godot;

namespace AbroDraft.WorldEntities;

public partial class PlayerCamera : Camera2D
{
	[Export] public Node2D TargetNode;
	[Export] public double SmoothingBase = 0.1;
	[Export] public double SmoothingPower = 2; // The power to which the SmoothingBase value will be raised
	
	public Vector2 TargetPosition = Vector2.Zero; // Position where camera wants to be
	public Vector2 ActualPosition = Vector2.Zero; // Position where camera is currently in
	public Vector2 PositionShift = Vector2.Zero; // Additional shift to ActualPosition
	// Note that PlayerCamera.Position is always being calculated to represent additional PositionShift
	// from ActualPosition (e.g. shake or punch).
	// ActualPosition represents main movement between current and desired position. It mostly used for custom smoothing.

	
	public override void _Ready()
	{
		ActualPosition = Position;
		TargetPosition = Position;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		TargetPosition = TargetNode.Position;
		var availableMovement = TargetPosition - ActualPosition;
		var actualMovement = availableMovement * Mathf.Pow(SmoothingBase, SmoothingPower);
		
		ActualPosition += actualMovement;
		
		Position = ActualPosition + PositionShift;
	}
}