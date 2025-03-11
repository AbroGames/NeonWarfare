using System.Collections.Generic;
using System.Linq;
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

    public ClientPlayer Player { get; private set; }

    public void OnPlayerSpawnPacket(SC_PlayerSpawnPacket playerSpawnPacket)
    {
        ClientPlayer player = CreateNetworkEntity<ClientPlayer>(
            ClientRoot.Instance.PackedScenes.Player, playerSpawnPacket.Nid);
        
        player.InitComponents();
        player.InitOnProfile(ClientRoot.Instance.Game.PlayerProfile);
        player.InitOnSpawnPacket(playerSpawnPacket.Position, playerSpawnPacket.Rotation, playerSpawnPacket.Color);
        
        AddChild(player);
        _allies.Add(player);
        _alliesByPeerId.Add(ClientRoot.Instance.Game.PlayerProfile.PeerId, player);
        Player = player;
        TreeExiting += () => RemoveAlly(player);
        TreeExiting += RemovePlayer;
        
        Camera.TargetNode = player;
    }

    public void RemovePlayer()
    {
        Player = null;
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnPlayerSpawnPacketListener(SC_PlayerSpawnPacket playerSpawnPacket) //TODO попробовать убрать static. Если убрать, то резолвер почему-то дважды вызывает этот метод. Возможно, из-за наследования.
    {
        ClientRoot.Instance.Game.World.OnPlayerSpawnPacket(playerSpawnPacket);
    }
}
