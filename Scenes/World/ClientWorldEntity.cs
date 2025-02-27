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
    
    public void OnStaticEntitySpawnPacket(SC_StaticEntitySpawnPacket staticEntitySpawnPacket)
    {
        Dictionary<SC_StaticEntitySpawnPacket.StaticEntityType, PackedScene> staticEntityScenesMap = new() 
        {
            { SC_StaticEntitySpawnPacket.StaticEntityType.Wall, ClientRoot.Instance.PackedScenes.Wall }
        };
        
        
        if (!staticEntityScenesMap.TryGetValue(staticEntitySpawnPacket.EntityType, out var entityScene))
        {
            Log.Error($"Received unknown type of StaticEntityType: {staticEntitySpawnPacket.EntityType}");
            return;
        }
        
        Node2D entity = entityScene.Instantiate<Node2D>();
        entity.Position = staticEntitySpawnPacket.Position;
        entity.Rotation = staticEntitySpawnPacket.Dir;
        entity.Scale = staticEntitySpawnPacket.Scale;
        entity.Modulate = staticEntitySpawnPacket.Color;
        AddChild(entity);
    }

    [EventListener(ListenerSide.Client)]
    public void OnStaticEntitySpawnPacketListener(SC_StaticEntitySpawnPacket staticEntitySpawnPacket)
    {
        ClientRoot.Instance.Game.World.OnStaticEntitySpawnPacket(staticEntitySpawnPacket);
    }

}
