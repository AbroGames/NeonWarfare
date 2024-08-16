﻿using System;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientWorld : Node2D
{
    [Export] [NotNull] public Floor Floor { get; set; }

    public Player Player;
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}