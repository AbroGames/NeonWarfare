using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public abstract partial class ServerWorld : Node2D
{

    public ServerNetworkEntityManager NetworkEntityManager = new();
    
    public T CreateAndAddToNetworkManager<T>(PackedScene scene) where T : Node
    {
        T newNode = scene.Instantiate<T>();
        NetworkEntityManager.AddEntity(newNode);
        return newNode;
    }
    
    public ServerPlayer SpawnPlayer(ServerPlayerProfile playerProfile) //TODO в readme о том, что мы такое стараемся использовать?
    {
        ServerPlayer player = CreateAndAddToNetworkManager<ServerPlayer>(ServerRoot.Instance.PackedScenes.Player);
        player.InitOnProfile(playerProfile);
        AddPlayer(player);
        player.Init();
        
        return player;
    }
}