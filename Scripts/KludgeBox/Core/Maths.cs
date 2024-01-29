using System;
using System.Runtime.CompilerServices;
using Godot;

namespace KludgeBox;

public static class Maths
{
    
    // Epsilons
    public const float Epsilon = 1E-06f;
    public const double Epsilon2 = 1E-14;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float Clamp(float value, float min, float max) => Mathf.Max(Mathf.Min(value, Mathf.Max(max, min)), Mathf.Min(min, max));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static double Clamp(double value, double min, double max) => Math.Clamp(value, Math.Min(min, max), Math.Max(min, max));
    
    /// <summary>
    /// Checks if two single-precision floating-point numbers are approximately equal.
    /// </summary>
    /// <param name="a">The first floating-point number.</param>
    /// <param name="b">The second floating-point number.</param>
    /// <returns>
    /// True if the numbers are approximately equal within a small tolerance, false otherwise.
    /// </returns>
    public static bool IsEqualApprox(this float a, float b)
    {
        if (a == b)
        {
            return true;
        }

        float tolerance = Epsilon * Mathf.Abs(a);
        if (tolerance < Epsilon)
        {
            tolerance = Epsilon;
        }

        return Mathf.Abs(a - b) < tolerance;
    }

    /// <summary>
    /// Checks if two double-precision floating-point numbers are approximately equal.
    /// </summary>
    /// <param name="a">The first floating-point number.</param>
    /// <param name="b">The second floating-point number.</param>
    /// <returns>
    /// True if the numbers are approximately equal within a small tolerance, false otherwise.
    /// </returns>
    public static bool IsEqualApprox(this double a, double b)
    {
        if (a == b)
        {
            return true;
        }

        double tolerance = Epsilon2 * Mathf.Abs(a);
        if (tolerance < Epsilon2)
        {
            tolerance = Epsilon2;
        }

        return Mathf.Abs(a - b) < tolerance;
    }
    
    
    
    
    /// <summary>
    /// Determines whether the specified value is a power of two.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is a power of two; otherwise, <c>false</c>.</returns>
    public static bool IsPowerOfTwo(int value) => value != 0 && (value & value - 1) == 0;

    /// <summary>
    /// Returns the next power of two that is greater than or equal to the specified value.
    /// </summary>
    /// <param name="value">The value to find the next power of two for.</param>
    /// <returns>The next power of two that is greater than or equal to the specified value.</returns>
    public static int NextPowerOfTwo(int value)
    {
        if (value == 0) return 1;
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    /// <summary>
    /// Determines whether the specified value is a power of two.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is a power of two; otherwise, <c>false</c>.</returns>
    public static bool IsPowerOfTwo(long value) => value != 0 && (value & (value - 1)) == 0;

    /// <summary>
    /// Returns the next power of two that is greater than or equal to the specified value.
    /// </summary>
    /// <param name="value">The value to find the next power of two for.</param>
    /// <returns>The next power of two that is greater than or equal to the specified value.</returns>
    public static long NextPowerOfTwo(long value)
    {
        if (value == 0) return 1;
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value |= value >> 32;
        return value + 1;
    }
    
    /// <summary>
    /// Maps a value from one range to another range.
    /// </summary>
    /// <param name="value">The value to map.</param>
    /// <param name="froma">The start of the original range.</param>
    /// <param name="toa">The end of the original range.</param>
    /// <param name="fromb">The start of the target range.</param>
    /// <param name="tob">The end of the target range.</param>
    /// <returns>The mapped value.</returns>
    public static float Map(float value, float froma, float toa, float fromb, float tob)
    {
        return fromb + (value - froma) * (tob - fromb) / (toa - froma);
    }

    /// <summary>
    /// Maps a value from the default range [0, 1] to a target range.
    /// </summary>
    /// <param name="value">The value to map.</param>
    /// <param name="from">The start of the target range.</param>
    /// <param name="to">The end of the target range.</param>
    /// <returns>The mapped value.</returns>
    public static float Map(float value, float from, float to)
    {
        return Map(value, 0, 1, from, to);
    }
    
    /// <summary>
    /// Calculates the number of digits in an integer.
    /// </summary>
    /// <param name="n">The integer value.</param>
    /// <returns>The number of digits in the integer.</returns>
    public static int Digits(int n)
    {
        return n < 100000
            ? n < 100
                ? n < 10 ? 1 : 2
                : n < 1000 ? 3
                    : n < 10000 ? 4
                        : 5
            : n < 10000000
                ? n < 1000000 ? 6 : 7
                : n < 100000000 ? 8
                    : n < 1000000000 ? 9 : 10;
    }

    /// <summary>
    /// Calculates the number of digits in a long integer.
    /// </summary>
    /// <param name="n">The long integer value.</param>
    /// <returns>The number of digits in the long integer.</returns>
    public static int Digits(long n)
    {
        return n == 0 ? 1 : (int)(Math.Log10((double)n) + 1);
    }
    
    /// <summary>
    /// Gets the Unit in the Last Place (ULP) of the specified double-precision floating-point number.
    /// </summary>
    /// <param name="value">The double-precision floating-point number.</param>
    /// <returns>The ULP of the specified double-precision floating-point number.</returns>
    /// <remarks>
    /// The Unit in the Last Place (ULP) is the difference between the smallest representable
    /// floating-point value greater than the given value and the value itself. It is a measure
    /// of the precision or the gap between consecutive floating-point numbers.
    /// </remarks>
    public static double GetUlp(this double value)
    {
        long bits = BitConverter.DoubleToInt64Bits(value);
        double nextValue = BitConverter.Int64BitsToDouble(bits + 1);
        double result = nextValue - value;
        return result;
    }

    /// <summary>
    /// Gets the Unit in the Last Place (ULP) of the specified single-precision floating-point number.
    /// </summary>
    /// <param name="value">The single-precision floating-point number.</param>
    /// <returns>The ULP of the specified single-precision floating-point number.</returns>
    /// <remarks>
    /// The Unit in the Last Place (ULP) is the difference between the smallest representable
    /// floating-point value greater than the given value and the value itself. It is a measure
    /// of the precision or the gap between consecutive floating-point numbers.
    /// </remarks>
    public static float GetUlp(this float value)
    {
        int bits = BitConverter.SingleToInt32Bits(value);
        float nextValue = BitConverter.Int32BitsToSingle(bits + 1);
        float result = nextValue - value;
        return result;
    }
}