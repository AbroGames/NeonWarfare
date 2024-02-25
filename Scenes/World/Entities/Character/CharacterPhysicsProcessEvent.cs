using KludgeBox.Events;

namespace AbroDraft.Scenes.World.Entities.Character;

public readonly struct CharacterPhysicsProcessEvent(Character character, double delta) : IEvent
{
    public Character Character { get; } = character;
    public double Delta { get; } = delta;
}