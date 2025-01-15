using System;
using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeonWarfare;

public abstract partial class ClientWorld : Node2D
{
    
    public override void _EnterTree()
    {
        AddFloor();
    }
}