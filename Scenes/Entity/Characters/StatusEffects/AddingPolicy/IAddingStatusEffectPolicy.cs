using System;
using System.Collections.Generic;

namespace NeonWarfare.Scenes.Entity.Characters.StatusEffects.AddingPolicy;

public interface IAddingStatusEffectPolicy
{
    public void OnAdd(
        Character character, 
        Character author, 
        StatusEffect newStatusEffect,
        IReadOnlyList<StatusEffect> currentStatusEffects,
        IReadOnlyDictionary<string, List<StatusEffect>> currentStatusEffectsById,
        IReadOnlyDictionary<string, List<StatusEffect>> currentStatusEffectsByTag,
        Action<StatusEffect, Character> addStatusEffectFunc, 
        Action<StatusEffect> removeStatusEffectFunc);
}