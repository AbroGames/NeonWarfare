using Godot;
using System;
using KludgeBox;
using Scenes.World;

public partial class FloatingLabel : Node2D
{
	private double _riseDist = 100;
	private double _targetTime = 1;
	private double _additionalTime = 0.3;

	private double _time = 0;
	private double _rads = Mathf.Pi / 2;

	private Vector2 _startPos;

	/// <inheritdoc />
	public override void _Ready()
	{
		_startPos = Position;
		double rotation = 10;
		Rotation += Mathf.DegToRad(Rand.Range(-rotation, rotation));
	}

	public void Configure(string text, Color color, double scale)
	{
		var label = GetNode("Label") as Label;
		var settings = label.LabelSettings;
		
		label.Text = text;
		settings.FontColor = color;
		settings.FontSize = (int)(settings.FontSize * scale);
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
