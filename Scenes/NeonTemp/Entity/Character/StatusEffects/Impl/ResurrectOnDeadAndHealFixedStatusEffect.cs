using System;
using System.Collections.Generic;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffects.AddingPolicy;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffects.Impl;

public class ResurrectOnDeadAndHealFixedStatusEffect : StatusEffect
{
    public readonly double Heal;
    public readonly double MaxHpMultBonus;
    
    protected ResurrectOnDeadAndHealFixedStatusEffect(DefaultInitParams defaultInitParams,
        double heal, double maxHpMultBonus)
        : base(defaultInitParams)
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
                _iconName ?? StatusEffectIconsStorageService.DefaultStatusEffect,
                _type,
                _isVisual,
                _addingPolicy ?? new LimitByIdAddingPolicy(),
                _time,
                _isFinishCondition);
        }
        
        #endregion
        
        private double _heal;
        private double? _maxHpMultBonus;
        
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

        public ResurrectOnDeadAndHealFixedStatusEffect Build()
        {
            return new ResurrectOnDeadAndHealFixedStatusEffect(
                BuildDefaultInitParams(),
                _heal,
                _maxHpMultBonus ?? 1);
        }
    }
}