using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect.AddingPolicy;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;

public abstract class AbstractStatusEffect
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string IconName { get; }
    public abstract IAddingStatusEffectPolicy AddingPolicy { get; }
        
    public bool IsFinished { get; protected set; } = false;
        
    public abstract void OnApplied(Character character, Character author);
    public abstract void OnRemoved(Character character);
    public abstract void OnPhysicsProcess(double delta);

    public abstract AbstractClientStatusEffect GetClientStatusEffect();
}