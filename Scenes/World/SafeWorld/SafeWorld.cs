using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

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
}