using Godot;
using System;

public partial class HpBar : ColorRect
{
	[ExportGroup("Hp Bar")] 
	[Export] public double Hp { get; set; } = 100;
	[Export] public double MaxHp { get; set; } = 100;
	
	[ExportGroup("Internal References")]
	[Export] [NotNull] public ColorRect RealBar { get; private set; }
	[Export] [NotNull] public ColorRect ImaginaryBar { get; private set; }
	[Export] [NotNull] public Label HpLabel { get; private set; }

	private double _imaginaryValue;
	private double Width => Size.X;
	private double Value => Hp / MaxHp;
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_imaginaryValue += (Value - _imaginaryValue) * 0.01;
		
		RealBar.CustomMinimumSize = Vec(Width * Value, 0);
		ImaginaryBar.CustomMinimumSize = Vec(Width * _imaginaryValue, 0);

		HpLabel.Text = $"Health: {Hp:N0} / {MaxHp:N0}";
	}
}
