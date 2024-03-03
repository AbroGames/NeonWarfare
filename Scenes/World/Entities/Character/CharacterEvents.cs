using KludgeBox.Events;

namespace KludgeBox.Events.Global.World;

public readonly record struct CharacterPhysicsProcessEvent(Character Character, double Delta) : IEvent;
public readonly record struct CharacterProcessEvent(Character Character, double Delta) : IEvent;
public readonly record struct CharacterReadyEvent(Character Character) : IEvent;

public readonly record struct CharacterDeathEvent(Character Character) : IEvent;

public class CharacterTakeDamageEvent(Character character, Damage damage) : CancellableEvent
{
    public Character Character { get; } = character;
    public Damage Damage { get; } = damage;
}

public readonly record struct CharacterApplyDamageRequest(Character Character, Damage Damage) : IEvent;