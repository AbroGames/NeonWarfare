using System;
using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;

namespace NeonWarfare.Scenes.World.Effects.CpuParticles;

public partial class CpuParticlesFx : Node2D
{
	[Export] [NotNull] public CpuParticles2D Particles2D { get; private set; }

	[Export] bool ScanEmittersRecursively = true;
	
	
	public Node2D Target { get; set; }
	public bool DoForceGlobalRotation { get; set; }
	public float ForcedRotation { get; set; }
	public Vector2 Offset { get; set; }
	
	public event Action Finished;
	public event Action Started;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		Particles2D.Emitting = true;
		RunAllParticles(this, ScanEmittersRecursively);
		Particles2D.Finished += () =>
		{
			Finished?.Invoke();
			QueueFree();
		};
		Started?.Invoke();
	}

	public void RunAllParticles(Node root, bool recursively = false)
	{
		var children = root.GetChildren();
		foreach (var child in children)
		{
			if (child is CpuParticles2D particles)
			{
				particles.Emitting = true;
			}

			if (recursively)
			{
				RunAllParticles(child, true);
			}
		}
	}
	
	public void UseGlobalRotation(float rotation)
	{
		ForcedRotation = rotation;
		DoForceGlobalRotation = true;
	}

	public override void _Process(double delta)
	{
		if (Target.IsValid())
		{
			GlobalPosition = Target.GlobalPosition + Offset;
		}
		
		
		if (DoForceGlobalRotation)
		{
			GlobalRotation = ForcedRotation;
		}
	}
}
