using Godot;

namespace KludgeBox.Events.Global;

public partial class GreenOverlay : TextureRect
{
	private double _ang = 0;
	private double _rot = 90;

	public override void _Process(double delta)
	{
		_ang += _rot * delta;
		_ang %= 180;
        
		var alpha = Mathf.Sin(Mathf.DegToRad(_ang));
        
		Modulate = Modulate with { A = 0.1f + (float)alpha * 0.1f };
	}
}