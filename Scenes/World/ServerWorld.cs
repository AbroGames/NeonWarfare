using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.World;

public abstract partial class ServerWorld : Node2D
{
    
    public void Init(List<ServerPlayerProfile> playerProfiles)
    {
        InitMap();
        InitPlayers(playerProfiles);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        CheckAllDeadAndRestart();
    }
}
