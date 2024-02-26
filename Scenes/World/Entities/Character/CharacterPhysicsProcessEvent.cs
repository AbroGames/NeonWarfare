using KludgeBox.Events;

namespace AbroDraft.World;

public readonly struct CharacterPhysicsProcessEvent(Character character, double delta) : IEvent
{
    public Character Character { get; } = character;
    public double Delta { get; } = delta;
}