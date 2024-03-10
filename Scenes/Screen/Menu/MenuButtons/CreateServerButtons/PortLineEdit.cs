using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

public partial class PortLineEdit : LineEdit
{
    public override void _Ready()
    {
        Text = DefaultNetworkSettings.Port.ToString();
    }

}