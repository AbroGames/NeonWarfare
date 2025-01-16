using Godot;

namespace NeonWarfare.Scenes.Screen.Components.Overlay;

/// <summary>
/// WARNING: Here be dragons! Be careful, as this node code will also run IN THE EDITOR.
/// </summary>
[Tool]
public partial class Overlay : TextureRect
{
	[Export]
	public Color Color
	{
		get => _color;
		set
		{
			_color = value;
			Reconfigure();
		}
	}

	[Export(PropertyHint.Range, "0, 1, 0.05")]
	public double Size
	{
		get => _size;
		set {
			_size = value;
			Reconfigure();
		}
	}

	
	[Export(PropertyHint.Range, "0, 10, 0.1, or_greater")]
    public double BlinkingInterval { get; set; } = 2; 
	
    
	[Export(PropertyHint.Range, "0, 1, 0.05")]
	public double MinAlpha { get; set; } = 0.1;
	
	[Export(PropertyHint.Range, "0, 1, 0.05")]
    public double MaxAlpha { get; set; } = 0.3;
	
	protected double _ang = 0;
	protected double _rot = 360;
	protected Gradient Gradient = new();
	protected double _size = 0.1;
	protected Color _color = Colors.White;
	protected GradientTexture2D GradientTexture => Texture as GradientTexture2D;
	protected double AlphaDiff => Mathf.Clamp(MaxAlpha - MinAlpha, 0, 1 - Mathf.Clamp(MinAlpha, 0, 1));

	protected double AnimationIntensity { get; set; }
	public override void _Ready()
	{
		GradientTexture.Gradient = Gradient;
		Reconfigure();
	} 
	
	private void Reconfigure()
	{
		Gradient.Colors = [Color with {A = 0}, Color];
		Gradient.Offsets = [(float)Mathf.Clamp(1-Size, 0, 1), 1];
	}

	public override void _Process(double delta)
	{
		ProcessAnimationIntensity(delta);
		
		var alpha = AnimationIntensity * AlphaDiff;
        
		Modulate = Modulate with { A = (float)(MinAlpha + alpha) };
	}

	protected virtual void ProcessAnimationIntensity(double delta)
	{
		_ang += _rot * delta / BlinkingInterval;
		_ang %= 360;
		AnimationIntensity = (1 + Mathf.Sin(Mathf.DegToRad(_ang))) / 2;
	}
}
