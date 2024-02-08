public class CharacterProcessEvent(Character character, double delta)
{
    public Character Character { get; private set; } = character;
    public double Delta { get; private set; } = delta;
}