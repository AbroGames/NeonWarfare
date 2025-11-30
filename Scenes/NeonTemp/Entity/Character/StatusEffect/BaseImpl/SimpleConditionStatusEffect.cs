using System;
using System.Collections.Generic;
using KludgeBox.Core.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.AddingPolicy;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.BaseImpl;

public class SimpleConditionStatusEffect : SimpleStatusEffect
    {
        protected readonly Func<Character, SimpleConditionStatusEffect, double, bool> IsFinishCondition;
        
        public SimpleConditionStatusEffect(string id, string displayName, string iconName,
            IAddingStatusEffectPolicy addingPolicy, ICollection<StatModifier<CharacterStat>> modifiers, 
            Func<Character, SimpleConditionStatusEffect, double, bool> isFinishCondition)
            : base(id,  displayName, iconName, addingPolicy, modifiers)
        {
            IsFinishCondition = isFinishCondition ?? throw new ArgumentNullException(nameof(isFinishCondition));
        }
        
        public override void OnPhysicsProcess(double delta)
        {
            if (IsFinishCondition(Character, this, delta)) IsFinished = true;
        }
        
        public static SimpleConditionStatusEffect Create(string id,
            Func<Character, SimpleConditionStatusEffect, double, bool> isFinishCondition, StatModifier<CharacterStat> modifier)
        {
            return new Builder().Id(id).Modifiers(modifier).IsFinishCondition(isFinishCondition).Build();
        }
        
        public static SimpleConditionStatusEffect Create(string id,
            Func<Character, SimpleConditionStatusEffect, double, bool> isFinishCondition, List<StatModifier<CharacterStat>> modifiers)
        {
            return new Builder().Id(id).Modifiers(modifiers).IsFinishCondition(isFinishCondition).Build();
        }
        
        public new class Builder
        {
            private string _id;
            private string _displayName;
            private string _iconName;
            private IAddingStatusEffectPolicy _addingPolicy;
            private readonly List<StatModifier<CharacterStat>> _modifiers = new();
            private Func<Character, SimpleConditionStatusEffect, double, bool> _isFinishCondition;

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

            public Builder IsFinishCondition(Func<Character, SimpleConditionStatusEffect, double, bool> isFinishCondition)
            {
                _isFinishCondition = isFinishCondition;
                return this;
            }

            public SimpleConditionStatusEffect Build()
            {
                return new SimpleConditionStatusEffect(
                    _id, 
                    _displayName ?? _id, 
                    _iconName ?? StatusEffectIconsStorageService.DefaultSimpleConditionStatusEffect,
                    _addingPolicy ?? new SkipCollisionIdAddingStatusEffectPolicy(), 
                    _modifiers,
                    _isFinishCondition);
            }
        }
    }