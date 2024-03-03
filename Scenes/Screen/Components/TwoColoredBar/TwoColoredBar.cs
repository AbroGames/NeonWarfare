using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class TwoColoredBar : ColorRect
{
	
	[Export] [NotNull] public ColorRect UpperBar { get; private set; }
	[Export] [NotNull] public ColorRect LowerBar { get; private set; }
	[Export] [NotNull] public Label Label { get; private set; }
	
	public double MaxValue { get; set; }
	public double CurrentUpperValue { get; set; }
	public double CurrentLowerValue { get; set; }
	
	public double CurrentUpperValuePercent => CurrentUpperValue / MaxValue;
	public double CurrentLowerValuePercent => CurrentLowerValue / MaxValue;
	public double Width => Size.X;
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		EventBus.Publish(new TwoColoredBarReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		EventBus.Publish(new TwoColoredBarProcessEvent(this, delta));
	}
}