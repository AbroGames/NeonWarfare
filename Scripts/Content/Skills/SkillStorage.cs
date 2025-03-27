using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scripts.Content.Skills.Impl;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Content.Skills;

public static class SkillStorage
{
    
    
    public enum SkillType
    {
        DefaultShot, Shotgun, DoubleShot, BurstShot, BurstDoubleShot //TODO create Skill classes, and to list and del this enum
    }

    private static readonly IReadOnlyList<Skill> Skills = new List<Skill>
    {
        new DefaultShotSkill()
    };

    private static readonly IReadOnlyDictionary<string, Skill> SkillMap = Skills.ToDictionary(skill => skill.SkillType, skill => skill);
    
    public static Skill GetSkill(string skillName)
    {
        if (!SkillMap.TryGetValue(skillName, out var skill))
        {
            Log.Error($"Not found Skill for unknown SkillName. SkillName = {skillName}");
        }
        return skill;
    }
}