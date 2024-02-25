using KludgeBox.Events;

namespace AbroDraft.Scenes.World.Entities.Character;

public readonly struct CharacterReadyEvent(Character character) : IEvent
{
    public Character Character { get; } = character;
}