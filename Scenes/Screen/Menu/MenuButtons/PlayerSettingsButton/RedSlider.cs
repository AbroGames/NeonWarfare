using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class RedSlider : HSlider
{
    public override void _Ready()
    {
        Value = Root.Instance.PlayerSettings.PlayerColor.R * 255f;
    }
}