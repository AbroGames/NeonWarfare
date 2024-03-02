using Godot;
using KludgeBox;

namespace KludgeBox.Events.Global.World;

public partial class SafeWorld : Node2D
{
	[Export] [NotNull] public Floor Floor { get; set; }
	public SafeHud SafeHud { get; set; }
	public Player Player;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		EventBus.Publish(new SafeWorldReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		EventBus.Publish(new SafeWorldProcessEvent(this, delta));
	}
}