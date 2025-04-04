using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ClientGame.MainScenes;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content.Skills;
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

    [EventListener(ListenerSide.Client)]
    public void OnLocationMeshPacket(SC_LocationMesh locationMeshPacket)
    {
        // Вручную генерируем и запекаем карту путей для отладочных целей.
        var region = new NavigationRegion2D();
        var polygon = new NavigationPolygon();
        var navSource = new NavigationMeshSourceGeometryData2D();
        polygon.AddOutline(locationMeshPacket.MeshVertices);
        
        NavigationServer2D.ParseSourceGeometryData(polygon, navSource, this);
        NavigationServer2D.BakeFromSourceGeometryData(polygon, navSource);
        region.NavigationPolygon = polygon;
        AddChild(region);
    }

    [EventListener(ListenerSide.Client)]
    public void OnUseSkillPacket(SC_UseSkillPacket useSkillPacket)
    {
        Skill.ClientSkillUseInfo useInfo = new Skill.ClientSkillUseInfo(
            World: this,
            Nid: useSkillPacket.Nid,
            CharacterPosition: useSkillPacket.CharacterPosition,
            CharacterRotation: useSkillPacket.CharacterRotation,
            CursorGlobalPosition: useSkillPacket.CursorGlobalPosition,
            Color: useSkillPacket.Color,
            CustomParams: useSkillPacket.CustomParams);
        
        SkillStorage.GetSkill(useSkillPacket.SkillType).OnClientUse(useInfo);
    }

}
