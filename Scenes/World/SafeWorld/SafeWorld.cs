using Godot;
using System.Collections.Generic;

public partial class SafeWorld : Node2D
{
	public SafeHud SafeHud { get; set; }
	public Player Player;
	
	public override void _Ready()
	{
		EventBus.Publish(new SafeWorldReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		EventBus.Publish(new SafeWorldProcessEvent(this, delta));
	}
}
