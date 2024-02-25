using Godot;

namespace AbroDraft.Scenes.World;

public sealed class Damage
{
    public Entities.Bullet.Bullet.AuthorEnum Author { get; set; }
    public Color LabelColor { get; set; }
    public double Amount { get; set; }
    public Entities.Character.Character Source { get; set; }

    public Damage(Entities.Bullet.Bullet.AuthorEnum author, Color labelColor, double amount, Entities.Character.Character source)
    {
        Author = author;
        LabelColor = labelColor;
        Amount = amount;
        Source = source;
    }
}