using Godot;
using NeonWarfare.Net;

namespace NeonWarfare;

public partial class PortLineEdit : LineEdit
{
    public override void _Ready()
    {
        Text = NetworkService.DefaultPort.ToString();
    }

}