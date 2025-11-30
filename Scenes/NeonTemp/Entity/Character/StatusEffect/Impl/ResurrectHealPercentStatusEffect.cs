using System;
using System.Collections.Generic;
using KludgeBox.Core.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.AddingPolicy;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.BaseImpl;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.Impl;

public class ResurrectHealPercentStatusEffect : SimpleTempStatusEffect
{
    public readonly double HealPercent;
    
    public ResurrectHealPercentStatusEffect(string id, string displayName, string iconName,
        IAddingStatusEffectPolicy addingPolicy, ICollection<StatModifier<CharacterStat>> modifiers, double time,
        double healPercent)
        : base(id, displayName, iconName, addingPolicy, modifiers, time)
    {
        if (healPercent <= 0) throw new ArgumentOutOfRangeException(nameof(healPercent));

        HealPercent = healPercent;
    }

    public override void OnPhysicsProcess(double delta)
    {
        base.OnPhysicsProcess(delta);
        if (Character.Stats.IsDead)
        {
            Resurrect();
            IsFinished = true;
        }
    }

    private void Resurrect()
    {
        Character.Stats.ResurrectWithPercentHeal(Author, HealPercent);
    }
    
    public new class Builder
    {
        private string _id;
        private string _displayName;
        private string _iconName;
        private IAddingStatusEffectPolicy _addingPolicy;
        private readonly List<StatModifier<CharacterStat>> _modifiers = new();
        private double _time;
        private double _healPercent;

        public Builder Id(string id)
        {
            _id = id;
            return this;
        }

        public Builder DisplayName(string displayName)
        {
            _displayName = displayName;
            return this;
        }
        
        public Builder IconName(string iconName)
        {
            _iconName = iconName;
            return this;
        }
        
        public Builder AddingPolicy(IAddingStatusEffectPolicy addingPolicy)
        {
            _addingPolicy = addingPolicy;
            return this;
        }
        
        public Builder Modifiers(StatModifier<CharacterStat> modifier)
        {
            _modifiers.Add(modifier);
            return this;
        }

        public Builder Modifiers(List<StatModifier<CharacterStat>> modifiers)
        {
            _modifiers.AddRange(modifiers);
            return this;
        }

        public Builder Time(double time)
        {
            _time = time;
            return this;
        }
        
        public Builder HealPercent(double healPercent)
        {
            _healPercent = healPercent;
            return this;
        }

        public ResurrectHealPercentStatusEffect Build()
        {
            return new ResurrectHealPercentStatusEffect(
                _id, 
                _displayName ?? _id, 
                _iconName ?? StatusEffectIconsStorageService.DefaultSimpleTempStatusEffect,
                _addingPolicy ?? new UpdateTimeAddingStatusEffectPolicy(), 
                _modifiers,
                _time,
                _healPercent);
        }
    }
}