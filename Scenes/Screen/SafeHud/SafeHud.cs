using Godot;
using KludgeBox;
using KludgeBox.Events.Global.World;
using SafeWorld = NeoVector.SafeWorld;

namespace KludgeBox.Events.Global;

public partial class SafeHud : Control
{
	[Export] [NotNull] public ProgressBar Xp { get; private set; }
	[Export] [NotNull] public TwoColoredBar HpBar { get; private set; }
	[Export] [NotNull] public Label XpLabel { get; private set; }
	[Export] [NotNull] public Label Level { get; private set; }
	[Export] [NotNull] public Label Fps { get; private set; }
	
	public SafeWorld SafeWorld { get; set; }
	
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