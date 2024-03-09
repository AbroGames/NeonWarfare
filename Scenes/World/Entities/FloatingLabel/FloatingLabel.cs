using Godot;
using KludgeBox;

namespace NeoVector;

public partial class FloatingLabel : Node2D
{
	private double _riseDist = 100;
	private double _targetTime = 1;
	private double _additionalTime = 0.3;

	private double _time = 0;
	private double _rads = Mathf.Pi / 2;

	private Vector2 _startPos;
	private double _scale;

	private Tween scaleTween;
	private Tween alphaTween;
	private Tween rotationTween;
	
	/// <inheritdoc />
	public override void _Ready()
	{
		var label = GetNode("Label") as Label;
		_startPos = Position;
		double rotation = 10;
		Rotation += Mathf.DegToRad(Rand.Range(-rotation, rotation));
		Scale = Vec(4);
		Modulate = Modulate with { A = 0 };
		scaleTween = GetTree().CreateTween();
		alphaTween = GetTree().CreateTween();
		rotationTween = GetTree().CreateTween()
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.InOut);
		scaleTween.TweenProperty(this, "scale", Vec(1), _targetTime * 0.2);
		alphaTween.TweenProperty(this, "modulate:a", 1, _targetTime);
		rotationTween.TweenProperty(this, "rotation", 
				Rotation + Mathf.DegToRad(Rand.Sign * rotation), _targetTime * 0.7);
		
		
	}

	public void Configure(string text, Color color, double scale)
	{
		var label = GetNode("Label") as Label;
		var settings = label.LabelSettings;
		_scale = scale;
		
		label.Text = text;
		settings.FontColor = color;
		settings.FontSize = (int)(settings.FontSize * _scale);
	}

	/// <inheritdoc />
	public override void _Process(double delta)
	{
		_time += delta;
		var clampedTime = Mathf.Min(_time, _targetTime);
		var fraction = clampedTime / _targetTime;
		
		Position = _startPos - Vec(0, _riseDist * Mathf.Sin(_rads * fraction));
		
		var label = GetNode("Label") as Label;

		var alphaFract = 1 - Mathf.Max((_time - _targetTime) / _additionalTime, 0);
		
		label.Modulate = label.Modulate with { A = (float)(1 * alphaFract) };
		
		if (_time >= _targetTime + _additionalTime)
		{
			QueueFree();
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationPredelete)
		{
			scaleTween.Kill();
			alphaTween.Kill();
			rotationTween.Kill();
		}
	}

	public static FloatingLabel Create()
	{
		return Root.Instance.PackedScenes.World.FloatingLabel.Instantiate<FloatingLabel>();
	}
	public static FloatingLabel Create(string text, Color color, double scale)
	{
		var label = Create();
		label.Configure(text, color, scale);
		return label;
	}
}