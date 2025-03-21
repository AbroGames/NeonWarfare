using Godot;
using System;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class Background3D : Node3D
{
    [Export] [NotNull] private OmniLight3D _light { get; set; }
    [Export] [NotNull] private StandardMaterial3D _material { get; set; }
    private Vector3 _startLightPosition = new(-4, 0, 15);
    private Vector3 _endLightPosition = new(-4, 0, -20);
    private float _lightSpeed = 10f;

    private float ExpectedTraverseTime => _startLightPosition.DistanceTo(_endLightPosition) / _lightSpeed;
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        PlayLightAnimation();
    }

    public void SetAccentColor(Color color)
    {
        _material.AlbedoColor = color;
        _material.Emission = color;
    }
    
    private void PlayLightAnimation()
    {
        _light.Position = _startLightPosition;
        var tween = CreateTween();
        tween.TweenProperty(_light, "position", _endLightPosition, ExpectedTraverseTime);
        tween.TweenCallback(Callable.From(PlayLightAnimation));
        tween.Play();
    }

}
