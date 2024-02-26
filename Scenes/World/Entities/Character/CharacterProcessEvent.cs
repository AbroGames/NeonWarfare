using KludgeBox.Events;

namespace AbroDraft.World;

public readonly struct CharacterProcessEvent(Character character, double delta) : IEvent
{
    public Character Character { get; } = character;
    public double Delta { get; } = delta;
}