﻿using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class PlayerSettingsButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.PlayerSettingsMenu);
        };
    }
}