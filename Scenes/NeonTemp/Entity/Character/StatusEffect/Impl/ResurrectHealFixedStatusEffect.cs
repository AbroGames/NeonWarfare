using System;
using System.Collections.Generic;
using KludgeBox.Core.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.AddingPolicy;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.BaseImpl;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.Impl;

public class ResurrectHealFixedStatusEffect : SimpleTempStatusEffect
{
    public readonly double Heal;
    public readonly double MaxHpMultBonus;
    
    public ResurrectHealFixedStatusEffect(string id, string displayName, string iconName,
        IAddingStatusEffectPolicy addingPolicy, ICollection<StatModifier<CharacterStat>> modifiers, double time,
        double heal, double maxHpMultBonus)
        : base(id, displayName, iconName, addingPolicy, modifiers, time)
    {
        if (heal <= 0) throw new ArgumentOutOfRangeException(nameof(heal));
        if (maxHpMultBonus <= 0) throw new ArgumentOutOfRangeException(nameof(maxHpMultBonus));

        Heal = heal;
        MaxHpMultBonus = maxHpMultBonus;
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
        Character.Stats.ResurrectWithFixedHeal(Author, Heal, MaxHpMultBonus);
    }
    
    public new class Builder
    {
        private string _id;
        private string _displayName;
        private string _iconName;
        private IAddingStatusEffectPolicy _addingPolicy;
        private readonly List<StatModifier<CharacterStat>> _modifiers = new();
        private double _time;
        private double _heal;
        private double? _maxHpMultBonus;

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
        
        public Builder Heal(double heal)
        {
            _heal = heal;
            return this;
        }
        
        public Builder MaxHpMultBonus(double maxHpMultBonus)
        {
            _maxHpMultBonus = maxHpMultBonus;
            return this;
        }

        public ResurrectHealFixedStatusEffect Build()
        {
            return new ResurrectHealFixedStatusEffect(
                _id, 
                _displayName ?? _id, 
                _iconName ?? StatusEffectIconsStorageService.DefaultSimpleTempStatusEffect,
                _addingPolicy ?? new UpdateTimeAddingStatusEffectPolicy(), 
                _modifiers,
                _time,
                _heal,
                _maxHpMultBonus ?? 1);
        }
    }
}