using KludgeBox.Events;

namespace AbroDraft.World;

public readonly struct CharacterReadyEvent(Character character) : IEvent
{
    public Character Character { get; } = character;
}