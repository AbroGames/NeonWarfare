using System;
using System.Collections.Generic;
using NeonWarfare.Scenes.Entity.Characters.Controller;
using NeonWarfare.Scenes.Entity.Characters.StatusEffects.AddingPolicy;

namespace NeonWarfare.Scenes.Entity.Characters.StatusEffects.Impl;

public class ControlBlockStatusEffect : StatusEffect
{
    
    public readonly ControlBlocker ControlBlocker;

    protected ControlBlockStatusEffect(DefaultInitParams defaultInitParams,
        ControlBlocker controlBlocker)
        : base(defaultInitParams)
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
        
        private ControlBlocker _controlBlocker;
        
        public Builder ControlBlocker(ControlBlocker controlBlocker)
        {
            _controlBlocker = controlBlocker;
            return this;
        }
        
        public ControlBlockStatusEffect Build()
        {
            return new ControlBlockStatusEffect(BuildDefaultInitParams(), _controlBlocker);
        }
    }
}