using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffects.AddingPolicy;

public class UpdateTimeAddingStatusEffectPolicy : IAddingStatusEffectPolicy
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
        if (currentStatusEffectsById.Count == 0)
        {
            addStatusEffectFunc(newStatusEffect, author);
            return;
        }
    
        if (currentStatusEffectsById.Count > 1)
        {
            throw new ArgumentException(
                $"{newStatusEffect} with {nameof(UpdateTimeAddingStatusEffectPolicy)}" +
                $"applied on {character}, which has many effects with id \"{newStatusEffect.Id}\" at the same time. " +
                $"It can be only if another effect with id \"{newStatusEffect.Id}\" use not {nameof(UpdateTimeAddingStatusEffectPolicy)} addingPolicy");
        }
    
        if (newStatusEffect.Cooldown != null)
        {
            StatusEffect currentStatusEffect = currentStatusEffectsById.GetValueOrDefault(newStatusEffect.Id, []).First();
            if (currentStatusEffect.Cooldown != null)
            {
                if (currentStatusEffect.Cooldown.TimeLeft < newStatusEffect.Cooldown.TimeLeft)
                {
                    removeStatusEffectFunc(currentStatusEffect);
                    addStatusEffectFunc(newStatusEffect, author);
                }
            }
            else
            {
                throw new ArgumentException(
                    $"{nameof(UpdateTimeAddingStatusEffectPolicy)} can used with another {nameof(StatusEffect)} with not null {nameof(StatusEffect.Cooldown)}");
            }
        }
        else
        {
            throw new ArgumentException(
                $"{nameof(UpdateTimeAddingStatusEffectPolicy)} can used only on {nameof(StatusEffect)} with not null {nameof(StatusEffect.Cooldown)}");
        }
    }
}