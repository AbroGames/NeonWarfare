using System;
using System.Collections.Generic;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public abstract partial class ClientWorld : Node2D
{
    
    public ClientNetworkEntityManager NetworkEntityManager = new();
    
    public override void _EnterTree()
    {
        AddFloor();
    }
}