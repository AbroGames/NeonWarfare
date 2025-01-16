using Godot;

namespace NeonWarfare.Scenes.Screen.Components.Overlay;

public partial class HitOverlay : Overlay
{
	[Export] public double AnimationTime = 2;
	private double _time = 1;
	
	private double AnimationState => _time / AnimationTime;
	
	/// <summary>
	/// Call this to trigger the animation
	/// </summary>
	public void DoHit()
	{
		_time = 0;
	}
	protected override void ProcessAnimationIntensity(double delta)
	{
		if (_time >= AnimationTime)
			return;
		
		_time += delta;
		
		_ang = _rot * AnimationState;
		AnimationIntensity = (1 + Mathf.Sin(Mathf.DegToRad(_ang))) / 2;
	}
}
