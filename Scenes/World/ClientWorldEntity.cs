using System;
using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeonWarfare;

public abstract partial class ClientWorld
{
    
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
        
        player.InitOnProfile(ClientRoot.Instance.Game.PlayerProfile);
        AddPlayer(player);
        player.OnSpawnPacket(playerSpawnPacket.X, playerSpawnPacket.Y, playerSpawnPacket.Dir);
    }

    public void OnAllySpawnPacket(SC_AllySpawnPacket allySpawnPacket)
    {
        ClientAlly ally = CreateNetworkEntity<ClientAlly>(
            ClientRoot.Instance.PackedScenes.Ally, allySpawnPacket.Nid);
        
        //TODO компонент с инерцией
        ally.InitOnProfile(ClientRoot.Instance.Game.AllyProfilesByPeerId[allySpawnPacket.Id]);
        AddAlly(ally);
        ally.OnSpawnPacket(allySpawnPacket.X, allySpawnPacket.Y, allySpawnPacket.Dir);
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
}