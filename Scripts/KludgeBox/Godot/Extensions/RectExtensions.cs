using Godot;
using KludgeBox.Structs;

namespace NeonWarfare.Scripts.KludgeBox.Godot.Extensions;

public static class RectExtensions
{
    public static Vector2[] ToPolygon(this Rect2 rect)
    {
        return [
            rect.Position, // top left
            rect.Position + Vec(rect.Size.X, 0), // top right
            rect.End, // bottom right
            rect.End - Vec(rect.Size.X, 0) // bottom left
        ];
    }
    
    
    public static Vector2I[] ToPolygon(this Rect2I rect)
    {
        return [
            rect.Position, // top left
            rect.Position + VecI(rect.Size.X, 0), // top right
            rect.End, // bottom right
            rect.End - VecI(rect.Size.X, 0) // bottom left
        ];
    }
    
    /// <summary>
    /// Gets the X-coordinate of the top-left corner of the specified Rect2.
    /// </summary>
    /// <param name="rect">The Rect2 object.</param>
    /// <returns>The X-coordinate of the top-left corner.</returns>
    public static real X(this Rect2 rect) => rect.Position.X;

    /// <summary>
    /// Gets the Y-coordinate of the top-left corner of the specified Rect2.
    /// </summary>
    /// <param name="rect">The Rect2 object.</param>
    /// <returns>The Y-coordinate of the top-left corner.</returns>
    public static real Y(this Rect2 rect) => rect.Position.Y;

    /// <summary>
    /// Gets the width of the specified Rect2.
    /// </summary>
    /// <param name="rect">The Rect2 object.</param>
    /// <returns>The width of the Rect2.</returns>
    public static real Width(this Rect2 rect) => rect.Size.X;

    /// <summary>
    /// Gets the height of the specified Rect2.
    /// </summary>
    /// <param name="rect">The Rect2 object.</param>
    /// <returns>The height of the Rect2.</returns>
    public static real Height(this Rect2 rect) => rect.Size.Y;

    public static real GetCircumradius(this Rect2 rect)
    {
        return Mathf.Sqrt(rect.Width() * rect.Width() + rect.Height() * rect.Height()) / 2;
    }

    public static Circle GetCircumcircle(this Rect2 rect)
    {
        var radius = rect.GetCircumradius();
        return new Circle(rect.GetCenter(), radius);
    }
}
