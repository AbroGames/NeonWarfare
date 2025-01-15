using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public abstract partial class ServerWorld 
{

    public ServerNetworkEntityManager NetworkEntityManager = new();
    
    public T CreateNetworkEntity<T>(PackedScene scene) where T : Node
    {
        T newNode = scene.Instantiate<T>();
        NetworkEntityManager.AddEntity(newNode);
        return newNode;
    }

    public void RemoveFromNetworkManager(Node node)
    {
        NetworkEntityManager.RemoveEntity(node);
    }
    
    public ServerPlayer SpawnPlayer(ServerPlayerProfile playerProfile) //TODO в readme о том, что мы такое стараемся использовать? Аналогично в ридми об обработке на клиенте в ClientWorld
    {
        ServerPlayer player = CreateNetworkEntity<ServerPlayer>(ServerRoot.Instance.PackedScenes.Player);
        player.InitOnProfile(playerProfile);
        AddPlayer(player);
        player.Init();
        
        return player;
    }
}