using System;
using System.Collections.Generic;
using System.Linq;
using KludgeBox.Core.Cooldown;
using MessagePack;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.AddingPolicy;
using NeonWarfare.Scenes.NeonTemp.Service;
using NeonWarfare.Scenes.NeonTemp.Stats;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.Impl;

public class SimpleTempStatusEffect : SimpleStatusEffect
{
    public readonly ManualCooldown Cooldown;
        
    public SimpleTempStatusEffect(string id, string displayName, string iconName,
        IAddingStatusEffectPolicy addingPolicy, ICollection<StatModifier<CharacterStat>> modifiers, double time)
        : base(id,  displayName, iconName, addingPolicy, modifiers)
    {
        if (time <= 0) throw new ArgumentOutOfRangeException(nameof(time));
        
        Cooldown = new(time, false, false, () => IsFinished = true);
    }

    public override void OnApplied(Character character)
    {
        base.OnApplied(character);
        Cooldown.Start();
    }

    public override void OnPhysicsProcess(double delta)
    {
        Cooldown.Update(delta);
    }
    
    public override AbstractClientStatusEffect GetClientStatusEffect()
    {
        return new Client(Id, DisplayName, IconName, Cooldown.TimeLeft);
    }
    
    public static SimpleTempStatusEffect Create(string id, double time, StatModifier<CharacterStat> modifier)
    {
        return new Builder().Id(id).Modifiers(modifier).Time(time).Build();
    }
    
    public static SimpleTempStatusEffect Create(string id, double time, List<StatModifier<CharacterStat>> modifiers)
    {
        return new Builder().Id(id).Modifiers(modifiers).Time(time).Build();
    }
    
    [MessagePackObject(AllowPrivate = true)]
    public new class Client : SimpleStatusEffect.Client
    {
        [Key(3)] public double Time { get; }
        [IgnoreMember] public readonly ManualCooldown Cooldown;

        [SerializationConstructor]
        public Client(string id, string displayName, string iconName, double time) :
            base(id, displayName, iconName)
        {
            Time = time;
            Cooldown = new ManualCooldown(Time, false, false);
        }

        public override void OnClientApplied(Character character)
        {
            Cooldown.Start();
        }
        
        public override void OnClientRemoved(Character character) { }

        public override void OnClientPhysicsProcess(double delta)
        {
            Cooldown.Update(delta);
        }
    }
    
    public new class Builder
    {
        private string _id;
        private string _displayName;
        private string _iconName;
        private IAddingStatusEffectPolicy _addingPolicy;
        private readonly List<StatModifier<CharacterStat>> _modifiers = new();
        private double _time;

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

        public SimpleTempStatusEffect Build()
        {
            return new SimpleTempStatusEffect(
                _id, 
                _displayName ?? _id, 
                _iconName ?? StatusEffectIconsStorageService.DefaultSimpleTempStatusEffect,
                _addingPolicy ?? new UpdateTimeAddingStatusEffectPolicy(), 
                _modifiers,
                _time);
        }
    }
    
    public class UpdateTimeAddingStatusEffectPolicy : IAddingStatusEffectPolicy
    {
        public void OnAdd(
            Character character, 
            AbstractStatusEffect newStatusEffect,
            Func<Dictionary<string, IReadOnlyCollection<AbstractStatusEffect>>> allCurrentStatusEffectsGetter,
            IReadOnlyCollection<AbstractStatusEffect> currentStatusEffectsById,
            Action<AbstractStatusEffect> addStatusEffectFunc, 
            Action<AbstractStatusEffect> removeStatusEffectFunc)
        {
            if (currentStatusEffectsById.Count == 0)
            {
                addStatusEffectFunc(newStatusEffect);
                return;
            }
        
            if (currentStatusEffectsById.Count > 1)
            {
                throw new ArgumentException(
                    $"{newStatusEffect} with {nameof(UpdateTimeAddingStatusEffectPolicy)}" +
                    $"applied on {character}, which has many effects with id \"{newStatusEffect.Id}\" at the same time. " +
                    $"It can be only if another effect with id \"{newStatusEffect.Id}\" use not {nameof(UpdateTimeAddingStatusEffectPolicy)} addingPolicy");
            }
        
            if (newStatusEffect is SimpleTempStatusEffect newTempStatusEffect)
            {
                if (currentStatusEffectsById.First() is SimpleTempStatusEffect currentStatusEffect)
                {
                    if (currentStatusEffect.Cooldown.TimeLeft < newTempStatusEffect.Cooldown.TimeLeft)
                    {
                        removeStatusEffectFunc(currentStatusEffect);
                        addStatusEffectFunc(newTempStatusEffect);
                    }
                }
                else
                {
                    throw new ArgumentException(
                        $"{nameof(UpdateTimeAddingStatusEffectPolicy)} can used with another {nameof(SimpleTempStatusEffect)}. " +
                        $"It can be only if another effect with id \"{newStatusEffect.Id}\" is not {nameof(SimpleTempStatusEffect)}");
                }
            }
            else
            {
                throw new ArgumentException(
                    $"{nameof(UpdateTimeAddingStatusEffectPolicy)} can used only on {nameof(SimpleTempStatusEffect)}");
            }
        }
    }
}