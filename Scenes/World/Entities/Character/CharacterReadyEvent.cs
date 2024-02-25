using KludgeBox.Events;

public readonly struct CharacterReadyEvent(Character character) : IEvent
{
    public Character Character { get; } = character;
}