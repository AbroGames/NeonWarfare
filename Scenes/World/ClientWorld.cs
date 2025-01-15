using System;
using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeonWarfare;

public abstract partial class ClientWorld : Node2D
{
    
    public ClientNetworkEntityManager NetworkEntityManager = new();
    
    public override void _EnterTree()
    {
        AddFloor();
    }

    public T CreateAndAddToNetworkManager<T>(PackedScene scene, long nid) where T : Node
    {
        T newNode = scene.Instantiate<T>();
        NetworkEntityManager.AddEntity(newNode, nid);
        return newNode;
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnPlayerSpawnPacketListener(ClientPlayer.SC_PlayerSpawnPacket playerSpawnPacket) //TODO попробовать убрать static. Перенести пакет в ClientWorld
    {
        ClientWorld world = ClientRoot.Instance.Game.World;
        ClientPlayer player = world.CreateAndAddToNetworkManager<ClientPlayer>(
            ClientRoot.Instance.PackedScenes.Player, playerSpawnPacket.Nid);
        
        player.InitOnProfile(ClientRoot.Instance.Game.PlayerProfile);
        world.AddPlayer(player);
        player.OnSpawnPacket(playerSpawnPacket.X, playerSpawnPacket.Y, playerSpawnPacket.Dir);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnAllySpawnPacketListener(ClientAlly.SC_AllySpawnPacket allySpawnPacket) //TODO  Перенести пакет в ClientWorld
    {
        ClientWorld world = ClientRoot.Instance.Game.World;
        ClientAlly ally = world.CreateAndAddToNetworkManager<ClientAlly>(
            ClientRoot.Instance.PackedScenes.Ally, allySpawnPacket.Nid);
        
        //TODO компонент с инерцией
        ally.InitOnProfile(ClientRoot.Instance.Game.AllyProfilesById[allySpawnPacket.Id]);
        world.AddAlly(ally);
        ally.OnSpawnPacket(allySpawnPacket.X, allySpawnPacket.Y, allySpawnPacket.Dir);
    }
}