using Godot;
using KludgeBox;

namespace NeonWarfare.Scenes.Screen.Components.TwoColoredBar;

public partial class TwoColoredBar : ColorRect
{
	
	[Export] [NotNull] public ColorRect UpperBar { get; private set; }
	[Export] [NotNull] public ColorRect LowerBar { get; private set; }
	[Export] [NotNull] public Label Label { get; private set; }
	
	public float MaxValue { get; set; }
	public float CurrentUpperValue { get; set; }
	public float CurrentLowerValue { get; set; }
	
	public float CurrentUpperValuePercent => CurrentUpperValue / MaxValue;
	public float CurrentLowerValuePercent => CurrentLowerValue / MaxValue;
	public float Width => Size.X;
	
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
