using System;
using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.World.Effects.CpuParticles;

public partial class CpuParticlesFx : Node2D
{
	
	[Export] [NotNull] public CpuParticles2D Particles2D { get; private set; }
	
	public event Action Finished;
	
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
