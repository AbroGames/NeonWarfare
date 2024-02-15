using System.Linq;
using Godot;

namespace KludgeBox.Godot;

public static class VectorExtensions
{
    /// <summary>
    /// Returns a Vector2 instance with specified x and y values.
    /// </summary>
    public static Vector2 Vec(real x, real y) { return new Vector2(x, y); }
    
    /// <summary>
    /// Returns a Vector2 instance with specified x and y values.
    /// </summary>
    public static Vector2 Vec(Vector2I veci) { return new Vector2(veci.X, veci.Y); }

    /// <summary>
    /// Returns a Vector2 instance with all values set to the specified value.
    /// </summary>
    public static Vector2 Vec(real xy) { return new Vector2(xy, xy); }

    /// <summary>
    /// Returns a Vector2 instance with all values set to zero.
    /// </summary>
    public static Vector2 Vec() { return Vector2.Zero; }


    /// <summary>
    /// Returns a Vector2I instance with specified x and y values.
    /// </summary>
    public static Vector2I VecI(int x, int y) { return new Vector2I(x, y); }

    /// <summary>
    /// Returns a Vector2I instance with all values set to the specified value.
    /// </summary>
    public static Vector2I VecI(int xy) { return new Vector2I(xy, xy); }

    /// <summary>
    /// Returns a Vector2I instance with all values set to zero.
    /// </summary>
    public static Vector2I VecI() { return Vector2I.Zero; }
    public static Vector2I VecI(Vector2 vec) { return VecI((int)vec.X, (int)vec.Y); }

    /// <summary>
    /// Gets the direction vector from the 'from' Vector2 to the 'to' Vector2.
    /// </summary>
    public static Vector2 DirectionFrom(this Vector2 to, Vector2 from) => from.DirectionTo(to);
    
    /// <summary>
    /// Extends the functionality of the Vector2 class by spinning it around a random angle.
    /// </summary>
    /// <param name="vector">The Vector2 instance to spin.</param>
    /// <returns>A new Vector2 instance representing the original vector rotated by a random angle.</returns>
    public static Vector2 Spin(this Vector2 vector)
    {
        real angle = Rand.Real * Mathf.Tau;
        vector = vector.Rotated(angle);

        return vector;
    }

    public static Vector2[] ToVector2Array(this Vector2I[] vecs) => vecs.Select(v => Vec(v.X, v.Y)).ToArray();
    public static Vector2I[] ToVector2IArray(this Vector2[] vecs) => vecs.Select(v => VecI((int)v.X, (int)v.Y)).ToArray();
}