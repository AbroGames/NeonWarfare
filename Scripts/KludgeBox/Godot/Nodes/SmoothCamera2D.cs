using Godot;

namespace NeonWarfare.Scripts.KludgeBox.Godot.Nodes;

[GlobalClass]
public partial class SmoothCamera2D : Camera2D
{
	[Export] public Node2D TargetNode;
	[Export] public real SmoothingBase = 0.1f;
	[Export] public real SmoothingPower = 2f; // The power to which the SmoothingBase value will be raised
	
	public Vector2 TargetPosition = Vector2.Zero; // Position where camera wants to be
	public Vector2 ActualPosition = Vector2.Zero; // Position where camera is currently in
	public Vector2 PositionShift = Vector2.Zero; // Additional shift to ActualPosition. WILL be smoothed. NOT usable for punching and shaking.
	public Vector2 HardPositionShift = Vector2.Zero; // Additional shift to ActualPosition. Will NOT be smoothed. USABLE for punching and shaking.
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
		var availableMovement = (TargetPosition + PositionShift) - ActualPosition;
		var actualMovement = availableMovement * Mathf.Pow(SmoothingBase, SmoothingPower);
		
		ActualPosition += actualMovement;
		
		Position = ActualPosition + HardPositionShift;
	}
}
