using Godot;
using System;

public partial class Icon : TextureRect
{
	[Export] [NotNull] public Label KeyLabel { get; private set; }
	[Export] [NotNull] public TextureRect IconImage { get; private set; }
	[Export] [NotNull] public TextureRect Overlay { get; private set; }
	[Export] [NotNull] public ColorRect CooldownOverlay { get; private set; }
	
	public double Progress
	{
		get => _process;
		set
		{
			((ShaderMaterial)CooldownOverlay.Material).SetShaderParameter("Progress", value);
			_process = value;
		}
	}

	public bool IsActive
	{
		get => !Overlay.Visible;
		set => Overlay.Visible = !value;
	}

	public char Key
	{
		get => KeyLabel.Text[0];
		set => KeyLabel.Text = value.ToString();
	}


	private double _process;
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	
	public override void _Process(double delta)
	{
	}
}
