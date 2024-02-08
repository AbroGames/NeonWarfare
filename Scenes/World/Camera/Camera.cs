using Godot;

public partial class Camera : Camera2D
{
	
	public Node2D TargetNode;
	
	public Vector2 TargetPosition = Vector2.Zero; // Position where camera wants to be
	public Vector2 ActualPosition = Vector2.Zero; // Position where camera is currently in
	public Vector2 PositionShift = Vector2.Zero; // Additional shift to ActualPosition
	public Vector2 HardPositionShift = Vector2.Zero; // Additional shift to ActualPosition that will not be smoothed. Usable for shake
	
	public double SmoothingBase = 0.1;
	public double SmoothingPower = 1.5; // The power to which the SmoothingBase value will be raised
	
	public override void _Ready()
	{
		Root.Instance.EventBus.Publish(new CameraReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		Root.Instance.EventBus.Publish(new CameraProcessEvent(this, delta));
	}
}