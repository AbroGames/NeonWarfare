public readonly struct CharacterReadyEvent(Character character)
{
    public Character Character { get; } = character;
}