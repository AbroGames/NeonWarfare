using System.Collections.Generic;
using KludgeBox.Core.Cooldown;
using MessagePack;

namespace NeonWarfare.Scenes.Entity.Character.StatusEffects;

[MessagePackObject]
public class ClientStatusEffect
{
    [Key(0)] public readonly string Id;
    [Key(1)] public readonly HashSet<string> Tags;
    [Key(2)] public readonly string DisplayName;
    [Key(3)] public readonly string Description;
    [Key(4)] public readonly string IconName;
    [Key(5)] public readonly StatusEffect.StatusEffectType Type;
    [Key(6)] public readonly bool IsVisual;
    [Key(7)] public readonly double? StartTime;
    
    [IgnoreMember] public readonly ManualCooldown Cooldown;
    
    [SerializationConstructor]
    public ClientStatusEffect(string id, HashSet<string> tags,
        string displayName, string description, string iconName, StatusEffect.StatusEffectType type, bool isVisual,
        double? startTime)
    {
        Id = id;
        Tags = tags;
        DisplayName = displayName;
        Description = description;
        IconName = iconName;
        Type = type;
        IsVisual = isVisual;
        StartTime = startTime;

        if (startTime.HasValue)
        {
            Cooldown = new ManualCooldown(StartTime.Value, false, false);
        }
    }
    
    public virtual void OnClientApplied(Character character)
    {
        Cooldown?.Start();
    }
        
    public virtual void OnClientRemoved(Character character) { }

    public virtual void OnClientPhysicsProcess(double delta)
    {
        Cooldown?.Update(delta);
    }
}