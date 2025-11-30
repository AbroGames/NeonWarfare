using System;
using System.Collections.Generic;
using KludgeBox.Core.Cooldown;
using KludgeBox.Core.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.AddingPolicy;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.BaseImpl;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.Impl;

public class HealStatusEffect : SimpleTempStatusEffect
{
    public readonly AutoCooldown HealCooldown;
    public readonly double HealValue;
    public readonly double MaxHpMultBonus;
    
    public HealStatusEffect(string id, string displayName, string iconName,
        IAddingStatusEffectPolicy addingPolicy, ICollection<StatModifier<CharacterStat>> modifiers, double time,
        double healValue, double healTime, double maxHpMultBonus)
        : base(id, displayName, iconName, addingPolicy, modifiers, time)
    {
        if (healValue <= 0) throw new ArgumentOutOfRangeException(nameof(healValue));
        if (healTime <= 0) throw new ArgumentOutOfRangeException(nameof(healTime));
        if (maxHpMultBonus <= 0) throw new ArgumentOutOfRangeException(nameof(maxHpMultBonus));

        HealValue = healValue;
        HealCooldown = new(healTime, false, Heal);
        MaxHpMultBonus = maxHpMultBonus;
    }
    
    public override void OnApplied(Character character, Character author)
    {
        base.OnApplied(character, author);
        HealCooldown.Start();
    }

    public override void OnPhysicsProcess(double delta)
    {
        base.OnPhysicsProcess(delta);
        HealCooldown.Update(delta);
    }

    private void Heal()
    {
        Character.Stats.Heal(Author, HealValue, MaxHpMultBonus);
    }
    
    public new class Builder
    {
        private string _id;
        private string _displayName;
        private string _iconName;
        private IAddingStatusEffectPolicy _addingPolicy;
        private readonly List<StatModifier<CharacterStat>> _modifiers = new();
        private double _time;
        private double _healValue;
        private double _healTime;
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
        
        public Builder HealValue(double healValue)
        {
            _healValue = healValue;
            return this;
        }
        
        public Builder HealTime(double healTime)
        {
            _healTime = healTime;
            return this;
        }
        
        public Builder MaxHpMultBonus(double maxHpMultBonus)
        {
            _maxHpMultBonus = maxHpMultBonus;
            return this;
        }

        public HealStatusEffect Build()
        {
            return new HealStatusEffect(
                _id, 
                _displayName ?? _id, 
                _iconName ?? StatusEffectIconsStorageService.DefaultSimpleTempStatusEffect,
                _addingPolicy ?? new UpdateTimeAddingStatusEffectPolicy(), 
                _modifiers,
                _time,
                _healValue,
                _healTime,
                _maxHpMultBonus ?? 1);
        }
    }
}