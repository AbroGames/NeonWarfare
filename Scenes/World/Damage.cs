using Godot;

namespace Scenes.World;

public sealed class Damage
{
    public Bullet.AuthorEnum Source { get; set; }
    public Color LabelColor { get; set; }
    public int Amount { get; set; }

    public Damage(Bullet.AuthorEnum source, Color labelColor, int amount)
    {
        Source = source;
        LabelColor = labelColor;
        Amount = amount;
    }
}