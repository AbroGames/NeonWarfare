using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.CreateServerInterface;

public partial class PortLineEdit : LineEdit
{
    public override void _Ready()
    {
        Text = Network.DefaultPort.ToString();
    }

}
