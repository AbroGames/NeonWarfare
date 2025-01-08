using System;
using System.Collections.Generic;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientWorld : Node2D
{
    
    public NetworkEntityManager NetworkEntityManager { get; private set; } = new();

    public override void _EnterTree()
    {
        AddFloor();
    }
}