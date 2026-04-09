using System;
using System.Collections.Generic;
using KludgeBox.Core.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffects.AddingPolicy;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffects.Impl;

public class StatChangeStatusEffect : StatusEffect
{

    public IReadOnlyList<StatModifier<CharacterStat>> Modifiers => _modifiers;
    private readonly List<StatModifier<CharacterStat>> _modifiers = new();
    
    protected StatChangeStatusEffect(DefaultInitParams defaultInitParams, 
        ICollection<StatModifier<CharacterStat>> modifiers) : 
        base(defaultInitParams)
    {
        if (modifiers == null || modifiers.Count == 0) throw new ArgumentNullException(nameof(modifiers));
        _modifiers.AddRange(modifiers);
    }
    
    public override void OnApplied(Character character, Character author)
    {
        base.OnApplied(character, author);
        foreach (var modifier in _modifiers)
        {
            character.Stats.AddStatModifier(modifier);
        }
    }
    
    public override void OnRemoved(Character character)
    {
        base.OnRemoved(character);
        foreach (var modifier in _modifiers)
        {
            character.Stats.RemoveStatModifier(modifier);
        }
    }
    
    public new class Builder
    {
        #region Copy from StatusEffect.Builder
        
        private string _id;
        private HashSet<string> _tags;
        private string _displayName;
        private string _description;
        private string _iconName;
        private StatusEffectType _type;
        private bool _isVisual;
        private IAddingStatusEffectPolicy _addingPolicy;
        private double? _time;
        private Func<StatusEffect, double, bool> _isFinishCondition;

        public Builder Id(string id)
        {
            _id = id;
            return this;
        }

        public Builder Tags(HashSet<string> tags)
        {
            _tags = tags;
            return this;
        }

        public Builder DisplayName(string displayName)
        {
            _displayName = displayName;
            return this;
        }

        public Builder Description(string description)
        {
            _description = description;
            return this;
        }

        public Builder IconName(string iconName)
        {
            _iconName = iconName;
            return this;
        }
        
        public Builder Type(StatusEffectType type)
        {
            _type = type;
            return this;
        }
        
        public Builder IsVisual(bool isVisual)
        {
            _isVisual = isVisual;
            return this;
        }

        public Builder AddingPolicy(IAddingStatusEffectPolicy addingPolicy)
        {
            _addingPolicy = addingPolicy;
            return this;
        }

        public Builder Time(double time)
        {
            _time = time;
            return this;
        }

        public Builder IsFinishCondition(Func<StatusEffect, double, bool> isFinishCondition)
        {
            _isFinishCondition = isFinishCondition;
            return this;
        }
        
        private DefaultInitParams BuildDefaultInitParams()
        {
            return new DefaultInitParams(
                _id,
                _tags,
                _displayName ?? _id,
                _description ?? _id,
                _iconName ?? Services.IconsStorage.StatusEffect.Default,
                _type,
                _isVisual,
                _addingPolicy ?? new LimitByIdAddingPolicy(),
                _time,
                _isFinishCondition);
        }
        
        #endregion
        
        private readonly List<StatModifier<CharacterStat>> _modifiers = new();
        
        public Builder Modifiers(StatModifier<CharacterStat> modifier)
        {
            _modifiers.Add(modifier);
            return this;
        }

        public Builder Modifiers(ICollection<StatModifier<CharacterStat>> modifiers)
        {
            _modifiers.AddRange(modifiers);
            return this;
        }
        
        public StatChangeStatusEffect Build()
        {
            return new StatChangeStatusEffect(BuildDefaultInitParams(), _modifiers);
        }
    }
}