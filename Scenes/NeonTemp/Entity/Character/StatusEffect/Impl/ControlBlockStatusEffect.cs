using System;
using System.Collections.Generic;
using KludgeBox.Core.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.AddingPolicy;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.BaseImpl;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.Impl;

public class ControlBlockStatusEffect : SimpleTempStatusEffect
{
    
    public readonly ControlBlocker ControlBlocker;

    public ControlBlockStatusEffect(string id, string displayName, string iconName,
        IAddingStatusEffectPolicy addingPolicy, ICollection<StatModifier<CharacterStat>> modifiers, double time,
        ControlBlocker controlBlocker)
        : base(id, displayName, iconName, addingPolicy, modifiers, time)
    {
        ControlBlocker = controlBlocker ?? throw new ArgumentNullException(nameof(controlBlocker));
    }
    
    public override void OnApplied(Character character, Character author)
    {
        base.OnApplied(character, author);
        character.Controller.AddBlock(ControlBlocker);
    }

    public override void OnRemoved(Character character)
    {
        base.OnRemoved(character);
        character.Controller.RemoveBlock(ControlBlocker);
    }

    public new class Builder
    {
        private string _id;
        private string _displayName;
        private string _iconName;
        private IAddingStatusEffectPolicy _addingPolicy;
        private readonly List<StatModifier<CharacterStat>> _modifiers = new();
        private double _time;
        private ControlBlocker _controlBlocker;

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
        
        public Builder ControlBlocker(ControlBlocker controlBlocker)
        {
            _controlBlocker = controlBlocker;
            return this;
        }
        
        public ControlBlockStatusEffect Build()
        {
            return new ControlBlockStatusEffect(
                _id, 
                _displayName ?? _id, 
                _iconName ?? StatusEffectIconsStorageService.DefaultSimpleTempStatusEffect,
                _addingPolicy ?? new UpdateTimeAddingStatusEffectPolicy(), 
                _modifiers,
                _time,
                _controlBlocker);
        }
    }
}