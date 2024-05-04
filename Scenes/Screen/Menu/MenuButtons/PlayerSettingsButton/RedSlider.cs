using Godot;

namespace NeonWarfare;

public partial class RedSlider : HSlider
{
    public override void _Ready()
    {
        Value = Root.Instance.PlayerSettings.PlayerColor.R * 255f;
    }
}