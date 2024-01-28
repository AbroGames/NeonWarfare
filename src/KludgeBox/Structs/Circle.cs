using System;
using Godot;
using Newtonsoft.Json;

namespace KludgeBox.Structs;

/// <summary>
/// Represents a circle in 2D space defined by its center <see cref="Position"/> and <see cref="Radius"/>.
/// </summary>
public struct Circle : IEquatable<Circle>
{
    /// <summary>
    /// The center position of the circle in 2D space.
    /// </summary>
    public Vector2 Position = Vec();

    /// <summary>
    /// The radius of the circle.
    /// </summary>
    public double Radius = 0f;

    /// <summary>
    /// Gets or sets the X coordinate of the circle's center position.
    /// </summary>
    [JsonIgnore]
    public double X
    {
        get => Position.X;
        set => Position = Position with { X = value };
    }

    /// <summary>
    /// Gets or sets the Y coordinate of the circle's center position.
    /// </summary>
    [JsonIgnore]
    public double Y
    {
        get => Position.Y;
        set => Position = Position with { Y = value };
    }

    /// <summary>
    /// Gets or sets the diameter of the circle.
    /// </summary>
    [JsonIgnore]
    public double Diameter
    {
        get => Radius * 2;
        set => Radius = value / 2;
    }

    /// <summary>
    /// Calculates and returns the area of the circle.
    /// </summary>
    [JsonIgnore]
    public double Area => Mathf.Pi * Mathf.Pow(Radius, 2);

    /// <summary>
    /// Creates a new instance of the <see cref="Circle"/> struct with the specified position and radius.
    /// </summary>
    /// <param name="position">The center position of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    public Circle(Vector2 position, double radius)
    {
        Position = position;
        Radius = radius;
    }

    public Circle()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Circle"/> struct with the specified radius and default position (0, 0).
    /// </summary>
    /// <param name="radius">The radius of the circle.</param>
    public Circle(double radius)
    {
        Radius = radius;
    }

    /// <summary>
    /// Gets the bounding rectangle (outer box) that encloses the circle.
    /// </summary>
    /// <returns>A <see cref="Rect2"/> representing the bounding rectangle of the circle.</returns>
    public Rect2 GetBoundingBox()
    {
        var topLeft = new Vector2(Position.X - Radius, Position.Y - Radius);
        var size = new Vector2(Radius * 2, Radius * 2);
        var box = new Rect2();
        box.Size = size;
        box.Position = topLeft;
        return box;
    }

    /// <summary>
    /// Checks if this circle intersects with another circle.
    /// </summary>
    /// <param name="other">The other circle to check for intersection.</param>
    /// <returns>True if the circles intersect; otherwise, false.</returns>
    public bool IntersectsWith(Circle other)
    {
        var distanceSquared = (Position.X - other.Position.X) * (Position.X - other.Position.X) +
                              (Position.Y - other.Position.Y) * (Position.Y - other.Position.Y);

        var radiusSum = Radius + other.Radius;
        return distanceSquared <= radiusSum * radiusSum;
    }

    /// <summary>
    /// Checks if this circle intersects with a rectangle.
    /// </summary>
    /// <param name="rect">The rectangle to check for intersection.</param>
    /// <returns>True if the circle intersects with the rectangle; otherwise, false.</returns>
    public bool IntersectsWith(Rect2 rect)
    {
        var closestX = Math.Max(rect.Position.X, Math.Min(Position.X, rect.Position.X + rect.Size.X));
        var closestY = Math.Max(rect.Position.Y, Math.Min(Position.Y, rect.Position.Y + rect.Size.Y));

        var distanceSquared = (Position.X - closestX) * (Position.X - closestX) +
                              (Position.Y - closestY) * (Position.Y - closestY);

        return distanceSquared <= Radius * Radius;
    }

    public bool Equals(Circle other)
    {
        if (Position.Equals(other.Position)) return Radius.Equals(other.Radius);
        return false;
    }

    public bool IsEqualsApprox(Circle other)
    {
        if (Position.IsEqualApprox(other.Position)) return Radius.IsEqualApprox(other.Radius);

        return false;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }

    public static Circle Parse(string text)
    {
        return JsonConvert.DeserializeObject<Circle>(text);
    }
}