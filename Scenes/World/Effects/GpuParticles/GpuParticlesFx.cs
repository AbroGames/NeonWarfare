using System;
using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.World.Effects.GpuParticles;

public partial class GpuParticlesFx : Node2D
{
	
	[Export] [NotNull] public GpuParticles2D Particles2D { get; private set; }
	
	public Node2D Target { get; set; }
	public bool DoForceGlobalRotation { get; set; }
	public float ForcedRotation { get; set; }
	public Vector2 Offset { get; set; }
	public bool UseGlobalOffset { get; set; }
	public event Action Finished;
	public event Action Started;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		Particles2D.Emitting = true;
		Particles2D.Finished += () =>
		{
			Finished?.Invoke();
			QueueFree();
		};
		Started?.Invoke();
	}

	public void UseGlobalRotation(float rotation)
	{
		ForcedRotation = rotation;
		DoForceGlobalRotation = true;
	}

	public override void _Process(double delta)
	{
		if (Target is not null)
		{
			if (UseGlobalOffset)
			{
				GlobalPosition = Target.GlobalPosition + Offset;
			}
			else
			{
				Position = Offset;
			}
		}

		if (DoForceGlobalRotation)
		{
			GlobalRotation = ForcedRotation;
		}
	}
}
