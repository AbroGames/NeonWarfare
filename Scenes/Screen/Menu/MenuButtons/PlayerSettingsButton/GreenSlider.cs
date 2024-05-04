using Godot;

namespace NeonWarfare;

public partial class GreenSlider : HSlider
{
    public override void _Ready()
    {
        Value = Root.Instance.PlayerSettings.PlayerColor.G * 255f;
    }
}