using Godot;
using System;
using NeonWarfare.Scenes.Screen;
using NeonWarfare.Scripts.KludgeBox.Networking;

public partial class ClassSelectionButton : Button
{
    [Export] private string _className;

    public override void _Ready()
    {
        Pressed += TryChangeClass;
    }

    private void TryChangeClass()
    {
        Network.SendToServer(new ChatNetworking.CS_SendMessagePacket($"/class {_className}"));
    }
}
