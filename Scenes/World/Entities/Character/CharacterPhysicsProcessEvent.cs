using KludgeBox.Events;

public readonly struct CharacterPhysicsProcessEvent(Character character, double delta) : IEvent
{
    public Character Character { get; } = character;
    public double Delta { get; } = delta;
}