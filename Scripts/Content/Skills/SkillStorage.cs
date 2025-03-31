using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scripts.Content.Skills.Impl;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Content.Skills;

public static class SkillStorage
{

    private static readonly IReadOnlyList<Skill> Skills = new List<Skill>
    {
        new DefaultShotSkill(),
        new ShotgunSkill(),
        new DoubleShotSkill()
    };

    private static readonly IReadOnlyDictionary<string, Skill> SkillByType = Skills.ToDictionary(skill => skill.SkillType, skill => skill);
    
    public static Skill GetSkill(string skillType)
    {
        if (!SkillByType.TryGetValue(skillType, out var skill))
        {
            Log.Error($"Not found Skill for unknown SkillName. SkillType = {skillType}");
        }
        return skill;
    }
}