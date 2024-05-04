using Godot;

namespace NeonWarfare;

public partial class RedSlider : HSlider
{
    public override void _Ready()
    {
        Value = ClientRoot.Instance.PlayerSettings.PlayerColor.R * 255f;
    }
}