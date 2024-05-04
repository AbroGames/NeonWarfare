using Godot;

namespace NeonWarfare;

public partial class BlueSlider : HSlider
{
    public override void _Ready()
    {
        Value = ClientRoot.Instance.PlayerSettings.PlayerColor.B * 255f;
    }
}