using Godot;
using KludgeBox.Networking;
using NeonWarfare.Net;

namespace NeonWarfare;

public partial class PortLineEdit : LineEdit
{
    public override void _Ready()
    {
        Text = Network.DefaultPort.ToString();
    }

}