using Godot;

namespace NeonWarfare;

public sealed class Damage
{
    public Bullet.AuthorEnum Author { get; set; }
    public Color LabelColor { get; set; }
    public double Amount { get; set; }
    public Character Source { get; set; }

    public Damage(Bullet.AuthorEnum author, Color labelColor, double amount, Character source)
    {
        Author = author;
        LabelColor = labelColor;
        Amount = amount;
        Source = source;
    }
}