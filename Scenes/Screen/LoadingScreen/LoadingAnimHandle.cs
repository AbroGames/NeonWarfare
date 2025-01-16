using Godot;
using System;

namespace NeonWarfare.Scenes.Screen.LoadingScreen;

public partial class LoadingAnimHandle : Node2D
{
	[Export] public Sprite2D ThickPart;
	[Export] public Sprite2D ThinPart;

	[Export] public Curve ThickRotationSpeedCurve;
	[Export] public Curve ThinRotationSpeedCurve;

	[Export] public float AnimationProgress = 0;
	[Export] public float AnimationSpeed = 0.1f;
	
	[Export] public float RotationSpeed = Mathf.Pi;


	/// <inheritdoc />
	public override void _Process(double delta)
	{
		AnimationProgress += AnimationSpeed * (float) delta;
		AnimationProgress %= 1;

		ThickPart.Rotation += RotationSpeed * ThickRotationSpeedCurve.Sample(AnimationProgress) * (float) delta;
		ThinPart.Rotation += RotationSpeed * ThinRotationSpeedCurve.Sample(AnimationProgress) * (float) delta;
	}
}
