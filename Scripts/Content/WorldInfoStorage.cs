using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Content;

public static class WorldInfoStorage
{
    
    public record WorldInfo(Func<PackedScene> ServerWorldScene, Func<PackedScene> ClientWorldMainScene);
    
    public enum WorldType
    {
        Unknown,
        Safe,
        Battle
    }

    public static readonly IReadOnlyDictionary<WorldType, WorldInfo> WorldInfoByType = new Dictionary<WorldType, WorldInfo>
    {
        { WorldType.Safe, new WorldInfo(() => ServerRoot.Instance.PackedScenes.SafeWorld, () => ClientRoot.Instance.PackedScenes.SafeWorldMainScene) },
        { WorldType.Battle, new WorldInfo(() => ServerRoot.Instance.PackedScenes.BattleWorld, () => ClientRoot.Instance.PackedScenes.BattleWorldMainScene) } 
    };
    
    public static WorldInfo GetWorldInfo(WorldType worldType)
    {
        if (!WorldInfoByType.TryGetValue(worldType, out var worldInfo))
        {
            Log.Error($"Not found WorldInfo for unknown WorldType. WorldType = {worldType}");
        }
        return worldInfo;
    }

    public static PackedScene GetClientMainScene(WorldType worldType)
    {
        return GetWorldInfo(worldType).ClientWorldMainScene.Invoke();
    }
    
    public static PackedScene GetServerScene(WorldType worldType)
    {
        return GetWorldInfo(worldType).ServerWorldScene.Invoke();
    }
}