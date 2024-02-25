using AbroDraft.Scripts.EventBus;
using Godot;
using KludgeBox;

namespace AbroDraft.Scenes.World.SafeWorld;

public partial class SafeWorld : Node2D
{
	[Export] [NotNull] public Floor Floor { get; set; }
	public Screen.SafeHud.SafeHud SafeHud { get; set; }
	public Entities.Character.Player.Player Player;
	
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