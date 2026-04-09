using System;
using System.Collections.Generic;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffects.AddingPolicy;

public class NoCheckAddingPolicy : IAddingStatusEffectPolicy
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
        addStatusEffectFunc(newStatusEffect, author);
    }
}