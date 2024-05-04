using System;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class CpuParticlesFx : Node2D
{
	public event Action Finished;
	[Export] [NotNull] public CpuParticles2D Particles2D { get; private set; }
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Particles2D.Emitting = true;
		Particles2D.Finished += () =>
		{
			Finished?.Invoke();
			QueueFree();
		};
	}
}