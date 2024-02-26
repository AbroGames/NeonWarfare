using System;
using Godot;
using KludgeBox;

namespace AbroDraft.World;

public partial class GpuParticlesFx : Node2D
{
	public event Action Finished;
	[Export] [NotNull] public GpuParticles2D Particles2D { get; private set; }
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