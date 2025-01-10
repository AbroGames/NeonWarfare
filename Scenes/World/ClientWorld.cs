using System;
using System.Collections.Generic;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public abstract partial class ClientWorld : Node2D
{
    
    public ClientNetworkEntityManager NetworkEntityManager;
    public OldNetworkEntityManager OldNetworkEntityManager { get; private set; } = new(); //TODO del
    
    public override void _EnterTree()
    {
        AddFloor();
    }
}