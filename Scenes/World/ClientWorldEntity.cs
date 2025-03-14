using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ClientGame.MainScenes;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld
{
    
    public ClientNetworkEntityManager NetworkEntityManager = new();
    
    public T CreateNetworkEntity<T>(PackedScene scene, long nid) where T : Node
    {
        T newNode = scene.Instantiate<T>();
        NetworkEntityManager.AddEntity(newNode, nid);
        return newNode;
    }
    
    [EventListener(ListenerSide.Client)]
    public void OnStaticEntitySpawnPacket(SC_StaticEntitySpawnPacket staticEntitySpawnPacket)
    {
        Node2D entity = staticEntitySpawnPacket.StaticEntityClientScene.Instantiate<Node2D>();
        entity.Position = staticEntitySpawnPacket.Position;
        entity.Rotation = staticEntitySpawnPacket.Rotation;
        entity.Scale = staticEntitySpawnPacket.Scale;
        entity.Modulate = staticEntitySpawnPacket.Color;
        AddChild(entity);
    }

}
