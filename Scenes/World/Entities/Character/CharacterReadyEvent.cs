using KludgeBox.Events;

namespace NeoVector.World;

public readonly struct CharacterReadyEvent(Character character) : IEvent
{
    public Character Character { get; } = character;
}