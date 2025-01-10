﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public abstract partial class ServerWorld : Node2D
{

    public ServerNetworkEntityManager NetworkEntityManager;
    public OldNetworkEntityManager OldNetworkEntityManager { get; private set; } = new(); //TODO del
  
}