using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class NickLineEdit : LineEdit
{
    public override void _Ready()
    {
        Text = Root.Instance.Game.PlayerInfo.PlayerName;
    }
}