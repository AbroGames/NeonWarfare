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
        /*Dictionary<ClientGame.SC_ChangeWorldPacket.ServerWorldType, PackedScene> worldScenesMap = new() 
        {
            { ClientGame.SC_ChangeWorldPacket.ServerWorldType.Safe, ClientRoot.Instance.PackedScenes.SafeWorldMainScene },
            { ClientGame.SC_ChangeWorldPacket.ServerWorldType.Battle, ClientRoot.Instance.PackedScenes.BattleWorldMainScene }
        };
		
        if (!worldScenesMap.TryGetValue(changeWorldPacket.WorldType, out var newWorldMainScene))
        {
            Log.Error($"Received unknown type of WorldMainScene: {changeWorldPacket.WorldType}");
            return;
        }
		
        ChangeMainScene(newWorldMainScene.Instantiate<IWorldMainScene>());*/
    }

    [EventListener(ListenerSide.Client)]
    public void OnStaticEntitySpawnPacketListener(SC_StaticEntitySpawnPacket staticEntitySpawnPacket)
    {
        ClientRoot.Instance.Game.World.OnStaticEntitySpawnPacket(staticEntitySpawnPacket);
    }

}
