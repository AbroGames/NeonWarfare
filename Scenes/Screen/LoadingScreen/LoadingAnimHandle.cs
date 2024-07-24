using Godot;
using System;

public partial class LoadingAnimHandle : Node2D
{
	[Export] public Sprite2D ThickPart;
	[Export] public Sprite2D ThinPart;

	[Export] public Curve ThickRotationSpeedCurve;
	[Export] public Curve ThinRotationSpeedCurve;

	[Export] public double AnimationProgress = 0;
	[Export] public double AnimationSpeed = 0.1;
	
	[Export] public double RotationSpeed = Mathf.Pi;


	/// <inheritdoc />
	public override void _Process(double delta)
	{
		AnimationProgress += AnimationSpeed * delta;
		AnimationProgress %= 1;

		ThickPart.Rotation += RotationSpeed * ThickRotationSpeedCurve.Sample(AnimationProgress) * delta;
		ThinPart.Rotation += RotationSpeed * ThinRotationSpeedCurve.Sample(AnimationProgress) * delta;
	}
}
