using KludgeBox.Events;

namespace NeoVector.World;

public readonly struct CharacterPhysicsProcessEvent(Character character, double delta) : IEvent
{
    public Character Character { get; } = character;
    public double Delta { get; } = delta;
}