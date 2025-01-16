using System;
using Godot;
using KludgeBox.Events;
using NeonWarfare;

public abstract partial class NetworkEntityComponent : Node
{
    public long Nid { get; set; } = -1; 
    
    public NetworkEntityComponent(long nid)
    {
        Nid = nid;
    }
}