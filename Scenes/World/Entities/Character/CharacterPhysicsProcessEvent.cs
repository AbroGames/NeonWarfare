public readonly struct CharacterPhysicsProcessEvent(Character character, double delta)
{
    public Character Character { get; } = character;
    public double Delta { get; } = delta;
}