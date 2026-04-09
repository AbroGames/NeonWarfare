using System;
using System.Collections.Generic;
using KludgeBox.Core.Cooldown;
using NeonWarfare.Scenes.Entity.Character.StatusEffects.AddingPolicy;

namespace NeonWarfare.Scenes.Entity.Character.StatusEffects;

public class StatusEffect
{
    public enum StatusEffectType
    {
        Neutral, Positive, Negative
    }

    public readonly string Id;
    public readonly HashSet<string> Tags;
    public readonly string DisplayName;
    public readonly string Description;
    public readonly string IconName;
    public readonly StatusEffectType Type;
    public readonly bool IsVisual;
    public readonly IAddingStatusEffectPolicy AddingPolicy;
    
    // Both parameters are optional. Without them, we create unlimited StatusEffect without self-destroy.
    public readonly ManualCooldown Cooldown;
    public readonly Func<StatusEffect, double, bool> IsFinishCondition;
        
    public bool IsFinished { get; protected set; }
    
    public Character Character { get; protected set; }
    public Character Author { get; protected set; }
    
    protected record DefaultInitParams(
        string Id, HashSet<string> Tags,
        string DisplayName, string Description, string IconName, StatusEffectType Type, bool IsVisual,
        IAddingStatusEffectPolicy AddingPolicy, 
        double? Time, Func<StatusEffect, double, bool> IsFinishCondition
    );
    
    protected StatusEffect(DefaultInitParams initParams)
    {
        if (initParams == null) throw new ArgumentNullException(nameof(initParams));
        
        Id = initParams.Id ?? throw new ArgumentNullException(nameof(initParams.Id));
        Tags = initParams.Tags ?? new();
        DisplayName = initParams.DisplayName ?? throw new ArgumentNullException(nameof(initParams.DisplayName));
        Description = initParams.Description ?? throw new ArgumentNullException(nameof(initParams.Description));
        IconName = initParams.IconName ?? throw new ArgumentNullException(nameof(initParams.IconName));
        Type = initParams.Type;
        IsVisual = initParams.IsVisual;
        AddingPolicy = initParams.AddingPolicy ?? throw new ArgumentNullException(nameof(initParams.AddingPolicy));

        if (initParams.Time.HasValue)
        {
            if (initParams.Time <= 0) throw new ArgumentOutOfRangeException(nameof(initParams.Time));
            Cooldown = new ManualCooldown(initParams.Time.Value, false, false, () => IsFinished = true);
        }

        IsFinishCondition = initParams.IsFinishCondition;
    }
        
    public virtual void OnApplied(Character character, Character author)
    {
        Character = character;
        Author = author;
        
        Cooldown?.Start();
    }

    public virtual void OnRemoved(Character character) { }

    public virtual void OnPhysicsProcess(double delta)
    {
        Cooldown?.Update(delta);
        if (IsFinishCondition != null && IsFinishCondition(this, delta)) IsFinished = true;
    }

    public virtual ClientStatusEffect GetClientStatusEffect()
    {
        return new ClientStatusEffect(Id, Tags, DisplayName, Description, IconName, Type, IsVisual, Cooldown?.TimeLeft);
    }
    
    public class Builder
    {
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

        public StatusEffect Build()
        {
            return new StatusEffect(BuildDefaultInitParams());
        }
    }
}