using System.Net.Sockets;
using Godot;

namespace Scenes.World;

public sealed class Damage
{
    public Bullet.AuthorEnum Author { get; set; }
    public Color LabelColor { get; set; }
    public double Amount { get; set; }
    public Vector2 Position { get; set; }
    public Character Source { get; set; }

    public Damage(Bullet.AuthorEnum author, Color labelColor, double amount, Vector2 hitPos, Character source)
    {
        Author = author;
        LabelColor = labelColor;
        Amount = amount;
        Position = hitPos;
        Source = source;
    }
}