using System;
using System.Collections.Generic;
using KludgeBox.Core.Cooldown;
using NeonWarfare.Scenes.Entity.Characters.StatusEffects.AddingPolicy;

namespace NeonWarfare.Scenes.Entity.Characters.StatusEffects.Impl;

public class HealStatusEffect : StatusEffect
{
    public readonly AutoCooldown HealCooldown;
    public readonly double HealValue;
    public readonly double MaxHpMultBonus;
    
    protected HealStatusEffect(DefaultInitParams defaultInitParams,
        double healValue, double healTime, double maxHpMultBonus)
        : base(defaultInitParams)
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
        
        private double _healValue;
        private double _healTime;
        private double? _maxHpMultBonus;
        
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
                BuildDefaultInitParams(),
                _healValue,
                _healTime,
                _maxHpMultBonus ?? 1);
        }
    }
}