using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Content;

public static class SkillInfoStorage
{
    
    public record SkillInfo(
        ActionInfoStorage.ActionType ActionType
        );
    
    public enum SkillType
    {
        DefaultShot, Shotgun, DoubleShot, BurstShot, BurstDoubleShot
    }
    
    private static readonly IReadOnlyDictionary<SkillType, SkillInfo> SkillInfoMap = new Dictionary<SkillType, SkillInfo>
    {
        { 
            SkillType.DefaultShot, 
            new SkillInfo(
                ActionType: ActionInfoStorage.ActionType.Shot
            )
        },
        { 
            SkillType.Shotgun, 
            new SkillInfo(
                ActionType: ActionInfoStorage.ActionType.Shot
            )
        },
        { 
            SkillType.DoubleShot, 
            new SkillInfo(
                ActionType: ActionInfoStorage.ActionType.Shot
            )
        },
        { 
            SkillType.BurstShot, 
            new SkillInfo(
                ActionType: ActionInfoStorage.ActionType.Shot
            )
        },
        { 
            SkillType.BurstDoubleShot, 
            new SkillInfo(
                ActionType: ActionInfoStorage.ActionType.Shot
            )
        }
    };
    
    public static SkillInfo GetSkillInfo(SkillType skillType)
    {
        if (!SkillInfoMap.TryGetValue(skillType, out var skillInfo))
        {
            Log.Error($"Not found SkillInfo for unknown SkillType. SkillType = {skillType}");
        }
        return skillInfo;
    }
}