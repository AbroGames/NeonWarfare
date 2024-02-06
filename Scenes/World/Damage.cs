using Godot;

namespace Scenes.World;

public sealed class Damage
{
    public Bullet.AuthorEnum Source { get; set; }
    public Color LabelColor { get; set; }
    public int Amount { get; set; }
    public Vector2 Position { get; set; }

    public Damage(Bullet.AuthorEnum source, Color labelColor, int amount, Vector2 hitPos)
    {
        Source = source;
        LabelColor = labelColor;
        Amount = amount;
        Position = hitPos;
    }
}