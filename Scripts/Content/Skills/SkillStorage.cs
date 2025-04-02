using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NeonWarfare.Scripts.Content.Skills.Impl;
using NeonWarfare.Scripts.KludgeBox;

namespace NeonWarfare.Scripts.Content.Skills;

public static class SkillStorage
{

    private static readonly IReadOnlyList<Skill> Skills = LoadSkills();
    private static readonly IReadOnlyDictionary<string, Skill> SkillByType = Skills.ToDictionary(skill => skill.SkillType, skill => skill);
    
    private static IReadOnlyList<Skill> LoadSkills()
    {
        var skillType = typeof(Skill);
        var skills = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => skillType.IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (Skill)Activator.CreateInstance(t))
            .ToList();

        Log.Info($"Loaded {skills.Count} skills");
        return skills.AsReadOnly();
    }
    
    public static Skill GetSkill(string skillType)
    {
        if (!SkillByType.TryGetValue(skillType, out var skill))
        {
            Log.Error($"Not found Skill for unknown SkillName. SkillType = {skillType}");
        }
        return skill;
    }
}