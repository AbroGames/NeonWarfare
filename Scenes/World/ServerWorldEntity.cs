using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare.Scenes.World;

public abstract partial class ServerWorld
{

    public ServerNetworkEntityManager NetworkEntityManager = new();
    
    public T CreateNetworkEntity<T>(PackedScene scene) where T : Node
    {
        T newNode = scene.Instantiate<T>();
        NetworkEntityManager.AddEntity(newNode);
        return newNode;
    }
    
    public ServerPlayer SpawnPlayer(ServerPlayerProfile playerProfile) 
    {
        ServerPlayer player = CreateNetworkEntity<ServerPlayer>(ServerRoot.Instance.PackedScenes.Player);
        player.AddChild(new NetworkInertiaComponent());
        player.InitOnProfile(playerProfile);
        AddChild(player);
        
        return player;
    }

    public ServerEnemy SpawnEnemy()
    {
        ServerEnemy enemy = CreateNetworkEntity<ServerEnemy>(ServerRoot.Instance.PackedScenes.Enemy);
        AddChild(enemy);

        return enemy;
    }
}
