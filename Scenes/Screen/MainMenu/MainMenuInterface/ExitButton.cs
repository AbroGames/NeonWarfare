﻿using Godot;

namespace NeonWarfare;

public partial class ExitButton : Button
{
    public override void _Ready()
    {
        Pressed += MenuButtonsService.ShutDown;
    }
}