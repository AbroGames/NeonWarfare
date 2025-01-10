using System;
using System.Collections.Generic;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientWorld : Node2D
{
    public Floor Floor { get; set; }

    public void AddFloor()
    {
        Floor = new Floor();
        Floor.Texture = GD.Load<Texture2D>(Sprites.Floor);
        AddChild(Floor);
    }
}