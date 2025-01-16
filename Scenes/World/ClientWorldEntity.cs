using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld
{
    public ClientPlayer Player => ClientRoot.Instance.Game.PlayerProfile.Player;
    public ClientNetworkEntityManager NetworkEntityManager = new();

    public T CreateNetworkEntity<T>(PackedScene scene, long nid) where T : Node
    {
        T newNode = scene.Instantiate<T>();
        NetworkEntityManager.AddEntity(newNode, nid);
        return newNode;
    }

    public void OnPlayerSpawnPacket(SC_PlayerSpawnPacket playerSpawnPacket)
    {
        ClientPlayer player = CreateNetworkEntity<ClientPlayer>(
            ClientRoot.Instance.PackedScenes.Player, playerSpawnPacket.Nid);
        player.AddChild(new ClientPlayerMovementComponent());
        player.AddChild(new ClientPlayerRotateComponent());
        
        player.InitOnProfile(ClientRoot.Instance.Game.PlayerProfile);
        AddChild(player);
        player.OnSpawnPacket(playerSpawnPacket.X, playerSpawnPacket.Y, playerSpawnPacket.Dir);
        
        Camera.TargetNode = player;
    }

    public void OnAllySpawnPacket(SC_AllySpawnPacket allySpawnPacket)
    {
        ClientAlly ally = CreateNetworkEntity<ClientAlly>(
            ClientRoot.Instance.PackedScenes.Ally, allySpawnPacket.Nid);
        ally.AddChild(new NetworkInertiaComponent());
        
        ally.InitOnProfile(ClientRoot.Instance.Game.AllyProfilesByPeerId[allySpawnPacket.Id]);
        AddChild(ally);
        ally.OnSpawnPacket(allySpawnPacket.X, allySpawnPacket.Y, allySpawnPacket.Dir);
    }
    
    public void OnEnemySpawnPacket(SC_EnemySpawnPacket enemySpawnPacket)
    {
        ClientEnemy enemy = CreateNetworkEntity<ClientEnemy>(
            ClientRoot.Instance.PackedScenes.Enemy, enemySpawnPacket.Nid);
        enemy.AddChild(new NetworkInertiaComponent());
        
        AddChild(enemy);
        enemy.OnSpawnPacket(enemySpawnPacket.X, enemySpawnPacket.Y, enemySpawnPacket.Dir);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnPlayerSpawnPacketListener(SC_PlayerSpawnPacket playerSpawnPacket) //TODO попробовать убрать static. Если убрать, то резолвер почему-то дважды вызывает этот метод. Возможно, из-за наследования.
    {
        ClientRoot.Instance.Game.World.OnPlayerSpawnPacket(playerSpawnPacket);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnAllySpawnPacketListener(SC_AllySpawnPacket allySpawnPacket)
    {
        ClientRoot.Instance.Game.World.OnAllySpawnPacket(allySpawnPacket);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnEnemySpawnPacketListener(SC_EnemySpawnPacket enemySpawnPacket)
    {
        ClientRoot.Instance.Game.World.OnEnemySpawnPacket(enemySpawnPacket);
    }
}
