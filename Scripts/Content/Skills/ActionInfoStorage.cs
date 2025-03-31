using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Content.Skills;

public static class ActionInfoStorage
{
    
    public record ActionInfo(
        Func<PackedScene> ClientScene, 
        Func<PackedScene> ServerScene
        );
    
    public enum ActionType
    {
        Shot, HealShot, ResurrectShot
    }
    
    private static readonly IReadOnlyDictionary<ActionType, ActionInfo> ActionInfoByType = new Dictionary<ActionType, ActionInfo>
    {
        { 
            ActionType.Shot, 
            new ActionInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.ShotAction, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.ShotAction
            )
        },
        { 
            ActionType.HealShot, 
            new ActionInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.HealShotAction, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.HealShotAction
            )
        },
        { 
            ActionType.ResurrectShot, 
            new ActionInfo(
                ClientScene: () => ClientRoot.Instance.PackedScenes.ResurrectShotAction, 
                ServerScene: () => ServerRoot.Instance.PackedScenes.ResurrectShotAction
            )
        }
    };
    
    public static ActionInfo GetActionInfo(ActionType actionType)
    {
        if (!ActionInfoByType.TryGetValue(actionType, out var actionInfo))
        {
            Log.Error($"Not found ActionInfo for unknown ActionType. ActionType = {actionType}");
        }
        return actionInfo;
    }

    public static PackedScene GetClientScene(ActionType actionType)
    {
        return GetActionInfo(actionType).ClientScene.Invoke();
    }
    
    public static PackedScene GetServerScene(ActionType actionType)
    {
        return GetActionInfo(actionType).ServerScene.Invoke();
    }
}