using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class BlueSlider : HSlider
{
    public override void _Ready()
    {
        Value = Root.Instance.PlayerInfo.PlayerColor.B * 255f;
    }
}