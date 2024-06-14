using Godot;
using KludgeBox.Net;

namespace NeonWarfare;

public partial class PortLineEdit : LineEdit
{
    public override void _Ready()
    {
        Text = DefaultNetworkSettings.Port.ToString();
    }

}