using System;
using System.Collections.Generic;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.AddingPolicy;

public interface IAddingStatusEffectPolicy
{
    public void OnAdd(
        Character character, 
        Character author, 
        AbstractStatusEffect newStatusEffect,
        Func<Dictionary<string, IReadOnlyCollection<AbstractStatusEffect>>> allCurrentStatusEffectsGetter,
        IReadOnlyCollection<AbstractStatusEffect> currentStatusEffectsById,
        Action<AbstractStatusEffect, Character> addStatusEffectFunc, 
        Action<AbstractStatusEffect> removeStatusEffectFunc);
}