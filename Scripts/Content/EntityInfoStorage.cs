using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Content;

public static class EntityInfoStorage
{
    
    public record StaticEntityInfo(Func<PackedScene> ClientScene, Func<PackedScene> ServerScene);
    
    public enum StaticEntityType
    {
        Wall, Border
    }

    public static readonly IReadOnlyDictionary<StaticEntityType, StaticEntityInfo> StaticEntityInfoByType = new Dictionary<StaticEntityType, StaticEntityInfo>
    {
        { StaticEntityType.Wall, new StaticEntityInfo(() => ClientRoot.Instance.PackedScenes.Wall, () => ServerRoot.Instance.PackedScenes.Wall) },
        { StaticEntityType.Border, new StaticEntityInfo(() => ClientRoot.Instance.PackedScenes.Wall, () => ServerRoot.Instance.PackedScenes.Wall) } 
    };
    
    public static StaticEntityInfo GetStaticEntityInfo(StaticEntityType staticEntityType)
    {
        if (!StaticEntityInfoByType.TryGetValue(staticEntityType, out var staticEntityInfo))
        {
            Log.Error($"Not found StaticEntityInfo for unknown StaticEntityType. StaticEntityType = {staticEntityType}");
        }
        return staticEntityInfo;
    }

    public static PackedScene GetStaticEntityClientScene(StaticEntityType staticEntityType)
    {
        return GetStaticEntityInfo(staticEntityType).ClientScene.Invoke();
    }
    
    public static PackedScene GetStaticEntityServerScene(StaticEntityType staticEntityType)
    {
        return GetStaticEntityInfo(staticEntityType).ServerScene.Invoke();
    }
}