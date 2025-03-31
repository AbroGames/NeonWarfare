using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Server;

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
    
    protected virtual void InitMap() { }
    
    protected void InitEntity(ClientWorld.SC_StaticEntitySpawnPacket staticEntitySpawnPacket)
    {
        Node2D entity = staticEntitySpawnPacket.StaticEntityServerScene.Instantiate<Node2D>();
        entity.Position = staticEntitySpawnPacket.Position;
        entity.Rotation = staticEntitySpawnPacket.Rotation;
        entity.Scale = staticEntitySpawnPacket.Scale;
        entity.Modulate = staticEntitySpawnPacket.Color;
        AddChild(entity);
    }
}
