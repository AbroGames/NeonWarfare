using Godot;
using System;
using NeonWarfare.Scenes.Screen;
using NeonWarfare.Scripts.KludgeBox.Networking;

public partial class WaveSelectionButton : Button
{
    [Export] private string _waveName;
    
    private WaveSelectionButton(){}
    public WaveSelectionButton(string waveName)
    {
        _waveName = waveName;
        Text = $"{_waveName} wave";
    }

    public override void _Ready()
    {
        Pressed += TryChangeWave;
    }

    private void TryChangeWave()
    {
        Network.SendToServer(new ChatNetworking.CS_SendMessagePacket($"/wave {_waveName}"));
    }
}
