using System;
using System.Collections.Generic;
using KludgeBox.Core.Cooldown;
using KludgeBox.Core.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.AddingPolicy;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.BaseImpl;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.Impl;

public class PoisonStatusEffect : SimpleTempStatusEffect
{
    public readonly AutoCooldown PoisonCooldown;
    public readonly double PoisonValue;
    public readonly bool ArmorIgnore;
    
    public PoisonStatusEffect(string id, string displayName, string iconName,
        IAddingStatusEffectPolicy addingPolicy, ICollection<StatModifier<CharacterStat>> modifiers, double time,
        double poisonValue, double poisonTime, bool armorIgnore)
        : base(id, displayName, iconName, addingPolicy, modifiers, time)
    {
        if (poisonValue <= 0) throw new ArgumentOutOfRangeException(nameof(poisonValue));
        if (poisonTime <= 0) throw new ArgumentOutOfRangeException(nameof(poisonTime));

        PoisonValue = poisonValue;
        PoisonCooldown = new(poisonTime, false, Damage);
        ArmorIgnore = armorIgnore;
    }
    
    public override void OnApplied(Character character, Character author)
    {
        base.OnApplied(character, author);
        PoisonCooldown.Start();
    }

    public override void OnPhysicsProcess(double delta)
    {
        base.OnPhysicsProcess(delta);
        PoisonCooldown.Update(delta);
    }

    private void Damage()
    {
        Character.Stats.Damage(Author, PoisonValue, ArmorIgnore);
    }
    
    public new class Builder
    {
        private string _id;
        private string _displayName;
        private string _iconName;
        private IAddingStatusEffectPolicy _addingPolicy;
        private readonly List<StatModifier<CharacterStat>> _modifiers = new();
        private double _time;
        private double _poisonValue;
        private double _poisonTime;
        private bool? _armorIgnore;

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
        
        public Builder PoisonValue(double poisonValue)
        {
            _poisonValue = poisonValue;
            return this;
        }
        
        public Builder PoisonTime(double poisonTime)
        {
            _poisonTime = poisonTime;
            return this;
        }
        
        public Builder ArmorIgnore(bool armorIgnore)
        {
            _armorIgnore = armorIgnore;
            return this;
        }

        public PoisonStatusEffect Build()
        {
            return new PoisonStatusEffect(
                _id, 
                _displayName ?? _id, 
                _iconName ?? StatusEffectIconsStorageService.DefaultSimpleTempStatusEffect,
                _addingPolicy ?? new UpdateTimeAddingStatusEffectPolicy(), 
                _modifiers,
                _time,
                _poisonValue,
                _poisonTime,
                _armorIgnore ?? false);
        }
    }
}