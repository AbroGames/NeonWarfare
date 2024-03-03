using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Events.Global.World;

namespace NeoVector;

public partial class SafeWorld : World
{
	public SafeHud SafeHud { get; set; }
	
	public override void _Ready()
	{
		base._Ready();
		EventBus.Publish(new SafeWorldReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		EventBus.Publish(new SafeWorldProcessEvent(this, delta));
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
	}
}