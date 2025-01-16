using Godot;
using KludgeBox;

namespace NeonWarfare.Scenes.World.Effects.FloatingLabel;

public partial class FloatingLabel : Node2D
{
	[Export] [NotNull] public Label Label { get; set; }
	
	private float _riseDist = 100;
	private float _targetTime = 1;
	private float _additionalTime = 0.3f;

	private double _time = 0;
	private float _rads = Mathf.Pi / 2;

	private Vector2 _startPos;
	private float _scale;

	private Tween _scaleTween;
	private Tween _alphaTween;
	private Tween _rotationTween;
	
	public override void _Ready()
	{
		_startPos = Position;
		float rotation = 10;
		Rotation += Mathf.DegToRad(Rand.Range(-rotation, rotation));
		Scale = Vec(4);
		Modulate = Modulate with { A = 0 };
		_scaleTween = GetTree().CreateTween();
		_alphaTween = GetTree().CreateTween();
		_rotationTween = GetTree().CreateTween()
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.InOut);
		_scaleTween.TweenProperty(this, "scale", Vec(1), _targetTime * 0.2);
		_alphaTween.TweenProperty(this, "modulate:a", 1, _targetTime);
		_rotationTween.TweenProperty(this, "rotation", 
				Rotation + Mathf.DegToRad(Rand.Sign * rotation), _targetTime * 0.7);
	}

	public void Configure(string text, Color color, float scale)
	{
		var settings = Label.LabelSettings;
		_scale = scale;
		
		Label.Text = text;
		settings.FontColor = color;
		settings.FontSize = (int)(settings.FontSize * _scale);
	}
	
	public override void _Process(double delta)
	{
		_time += delta;
		var clampedTime = Mathf.Min(_time, _targetTime);
		var fraction = clampedTime / _targetTime;
		
		Position = _startPos - Vec(0, _riseDist * (float) Mathf.Sin(_rads * fraction));

		var alphaFract = 1 - Mathf.Max((_time - _targetTime) / _additionalTime, 0);
		
		Label.Modulate = Label.Modulate with { A = (float)(1 * alphaFract) };
		
		if (_time >= _targetTime + _additionalTime)
		{
			QueueFree();
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationPredelete)
		{
			_scaleTween.Kill();
			_alphaTween.Kill();
			_rotationTween.Kill();
		}
	}
}
