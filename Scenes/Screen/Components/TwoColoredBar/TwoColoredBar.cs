using Godot;
using KludgeBox;

namespace NeonWarfare;

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
	}

	public override void _Process(double delta)
	{
		UpperBar.CustomMinimumSize = Vec(Width * CurrentUpperValuePercent, 0);
		LowerBar.CustomMinimumSize = Vec(Width * CurrentLowerValuePercent, 0);
	}
}