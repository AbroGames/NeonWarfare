using KludgeBox.Events;

namespace NeoVector.World;

public readonly struct CharacterProcessEvent(Character character, double delta) : IEvent
{
    public Character Character { get; } = character;
    public double Delta { get; } = delta;
}