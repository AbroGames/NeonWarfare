using Godot;
using System;
using System.Collections.Generic;
using NeonWarfare.Scenes.Root.ClientRoot;

public partial class WaveContainer : VBoxContainer
{
    public override void _Ready()
    {
        Visible = ClientRoot.Instance.Game.PlayerProfile.IsAdmin;
        List<string> types = [
            "def",
            "zerg",
            "shooter",
            "turtle",
            "shooter-turtle",
            "boss",
            "only-boss"
        ];

        foreach (var waveType in types)
        {
            var btn = new WaveSelectionButton(waveType);
            AddChild(btn);
        }
    }
}
