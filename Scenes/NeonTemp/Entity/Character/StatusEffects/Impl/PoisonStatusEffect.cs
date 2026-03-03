using System;
using System.Collections.Generic;
using KludgeBox.Core.Cooldown;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffects.AddingPolicy;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffects.Impl;

public class PoisonStatusEffect : StatusEffect
{
    public readonly AutoCooldown PoisonCooldown;
    public readonly double PoisonValue;
    public readonly bool ArmorIgnore;
    
    protected PoisonStatusEffect(DefaultInitParams defaultInitParams,
        double poisonValue, double poisonTime, bool armorIgnore)
        : base(defaultInitParams)
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
        
        private double _poisonValue;
        private double _poisonTime;
        private bool? _armorIgnore;
        
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
                BuildDefaultInitParams(),
                _poisonValue,
                _poisonTime,
                _armorIgnore ?? false);
        }
    }
}