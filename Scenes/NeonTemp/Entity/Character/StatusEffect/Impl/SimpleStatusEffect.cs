using System;
using System.Collections.Generic;
using MessagePack;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.AddingPolicy;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.Impl;

public class SimpleStatusEffect : AbstractStatusEffect
    {
        public override string Id { get; }
        public override string DisplayName { get; }
        public override string IconName { get; }
        public override IAddingStatusEffectPolicy AddingPolicy { get; }
        
        protected Character Character;
        protected readonly List<StatModifier> Modifiers = new();
        
        public SimpleStatusEffect(string id, string displayName, string iconName,
            IAddingStatusEffectPolicy addingPolicy, ICollection<StatModifier> modifiers)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            IconName = iconName ?? throw new ArgumentNullException(nameof(iconName));
            AddingPolicy = addingPolicy ?? throw new ArgumentNullException(nameof(addingPolicy));
            
            if (modifiers == null || modifiers.Count == 0) throw new ArgumentNullException(nameof(modifiers));
            Modifiers.AddRange(modifiers);
        }
        
        public override void OnApplied(Character character)
        {
            Character = character;
            foreach (var modifier in Modifiers)
            {
                character.Stats.GetContainer().AddStatModifier(modifier);
            }
        }

        public override void OnRemoved(Character character)
        {
            foreach (var modifier in Modifiers)
            {
                character.Stats.GetContainer().RemoveStatModifier(modifier);
            }
        }

        public override void OnPhysicsProcess(double delta) { }

        public override AbstractClientStatusEffect GetClientStatusEffect()
        {
            return new Client(Id, DisplayName, IconName);
        }

        public static SimpleStatusEffect Create(string id, StatModifier modifier)
        {
            return new Builder().Id(id).Modifiers(modifier).Build();
        }
        
        public static SimpleStatusEffect Create(string id, List<StatModifier> modifiers)
        {
            return new Builder().Id(id).Modifiers(modifiers).Build();
        }
        
        [MessagePackObject(AllowPrivate = true)]
        public class Client : AbstractClientStatusEffect
        {
            [SerializationConstructor]
            public Client(string id, string displayName, string iconName)
            {
                Id = id;
                DisplayName = displayName;
                IconName = iconName;
            }
            
            public override void OnClientApplied(Character character) { }
            public override void OnClientRemoved(Character character) { }
            public override void OnClientPhysicsProcess(double delta) { }
        }

        public class Builder
        {
            private string _id;
            private string _displayName;
            private string _iconName;
            private IAddingStatusEffectPolicy _addingPolicy;
            private readonly List<StatModifier> _modifiers = new();

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
            
            public Builder Modifiers(StatModifier modifier)
            {
                _modifiers.Add(modifier);
                return this;
            }

            public Builder Modifiers(List<StatModifier> modifiers)
            {
                _modifiers.AddRange(modifiers);
                return this;
            }

            public SimpleStatusEffect Build()
            {
                return new SimpleStatusEffect(
                    _id, 
                    _displayName ?? _id, 
                    _iconName ?? StatusEffectIconsStorageService.DefaultSimpleStatusEffect,
                    _addingPolicy ?? new SkipCollisionIdAddingStatusEffectPolicy(), 
                    _modifiers);
            }
        }
    }