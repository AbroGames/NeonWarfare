using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffects.AddingPolicy;

public class LimitByTagAddingPolicy(string tagForLimit, int limit = 1) : IAddingStatusEffectPolicy
{
    public void OnAdd(
        Character character, 
        Character author, 
        StatusEffect newStatusEffect,
        IReadOnlyList<StatusEffect> currentStatusEffects,
        IReadOnlyDictionary<string, List<StatusEffect>> currentStatusEffectsById,
        IReadOnlyDictionary<string, List<StatusEffect>> currentStatusEffectsByTag,
        Action<StatusEffect, Character> addStatusEffectFunc, 
        Action<StatusEffect> removeStatusEffectFunc)
    {
        if (currentStatusEffectsByTag.GetValueOrDefault(tagForLimit, []).Count < limit)
        {
            addStatusEffectFunc(newStatusEffect, author);
        }
    }
}