using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class GreenSlider : HSlider
{
    public override void _Ready()
    {
        Value = Root.Instance.PlayerInfo.PlayerColor.G * 255f;
    }
}