using Godot;
using KludgeBox.DI.Requests;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Background;

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
        Di.Process(this);
        SetAccentColor(Services.GameSettings.GetSettings().PlayerColor);
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
