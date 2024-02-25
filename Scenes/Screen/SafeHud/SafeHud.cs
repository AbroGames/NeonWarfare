using AbroDraft.Scripts.EventBus;
using Godot;
using KludgeBox;

namespace AbroDraft.Scenes.Screen.SafeHud;

public partial class SafeHud : Control
{
	[Export] [NotNull] public ProgressBar Xp { get; private set; }
	[Export] [NotNull] public Components.TwoColoredBar.TwoColoredBar HpBar { get; private set; }
	[Export] [NotNull] public Label XpLabel { get; private set; }
	[Export] [NotNull] public Label Level { get; private set; }
	[Export] [NotNull] public Label Fps { get; private set; }
	
	public World.SafeWorld.SafeWorld SafeWorld { get; set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		EventBus.Publish(new SafeHudReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		EventBus.Publish(new SafeHudProcessEvent(this, delta));
	}
}