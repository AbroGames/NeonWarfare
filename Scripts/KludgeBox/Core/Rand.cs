using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox.Structs;

namespace NeonWarfare.Scripts.KludgeBox.Core;

/// <summary>
/// Represents a pseudo-random number generator with the ability to save and restore its state.
/// </summary>
/// <remarks>
/// This class provides methods to save and restore the state of the pseudo-random number generator,
/// allowing the generation of deterministic random sequences.
/// </remarks>
public static class Rand
{
	/// <summary>
	/// Returns a pseudo-random float between 0.0 and 1.0.
	/// </summary>
	public static float Float => (float)Double;
		
		
	/// <summary>
	/// Returns a pseudo-random double between 0.0 and 1.0.
	/// </summary>
	public static double Double => ((MurmurHash.GetInt(_seed, _iterations++) - -2147483648.0) / 4294967295.0);
	
#if GODOT_REAL_T_IS_DOUBLE
	public static double Real => Double;
#else
	public static float Real => Float;
#endif

	/// <summary>
	/// Gets a random boolean value, true with a 50% chance and false with a 50% chance.
	/// </summary>
	public static bool Bool => Double < 0.5;

	/// <summary>
	/// Gets the sign of a random value: -1 if the random value is less than 0, or 1 otherwise.
	/// </summary>
	public static int Sign
	{
		get
		{
			if (!Bool)
			{
				return -1;
			}
			return 1;
		}
	}
	/// <summary>
	/// Gets a random integer value.
	/// </summary>
	public static int Int => MurmurHash.GetInt(_seed, _iterations++);

	/// <summary>
	/// Generates a random unit vector in 2D space.
	/// </summary>
	/// <returns>A random unit vector in 2D space.</returns>
	public static Vector2 UnitVector => new Vector2((real)Gaussian(), (real)Gaussian()).Normalized();
		

	/// <summary>
	/// Generates a random vector inside the unit circle in 2D space.
	/// </summary>
	/// <returns>A random vector inside the unit circle in 2D space.</returns>
	public static Vector2 InsideUnitCircle
	{
		get
		{
			Vector2 result;
			do
			{
				result = new Vector2(Real - 0.5f, Real - 0.5f) * 2f;
			}
			while (!(result.LengthSquared() <= 1f));
			return result;
		}
	}
		

	/// <summary>
	/// Gets or sets the compressed state of the pseudo-random number generator.
	/// </summary>
	/// <value>
	/// A <see cref="ulong"/> value representing the compressed state of the generator.
	/// </value>
	private static ulong StateCompressed
	{
		get
		{
			// Combine the seed and iterations into a 64-bit unsigned integer
			return _seed | ((ulong)_iterations << 32);
		}
		set
		{
			// Extract the seed and iterations from the compressed state
			_seed = (uint)(value & 0xFFFFFFFFu);
			_iterations = (uint)((value >> 32) & 0xFFFFFFFFu);
		}
	}

	/// <summary>
	/// Sets the seed for the pseudo-random number generator and resets the iteration count.
	/// </summary>
	/// <value>
	/// An integer value representing the seed for the generator.
	/// </value>
	public static int Seed
	{
		set
		{
			_seed = (uint)value;
			_iterations = 0u;
		}
	}

	// Private fields to hold the state and stack for saving and restoring state.
	private static uint _seed;
	private static uint _iterations;
	private static Stack<ulong> _stateStack = new Stack<ulong>();

	static Rand()
	{
		_seed = (uint)DateTime.Now.GetHashCode();
	}

	/// <summary>
	/// Generates a random vector inside an annulus (a region between two concentric circles) in 2D space.
	/// </summary>
	/// <param name="innerRadius">The inner radius of the annulus.</param>
	/// <param name="outerRadius">The outer radius of the annulus.</param>
	/// <returns>A random vector inside the annulus in 2D space.</returns>
	public static Vector2 InsideAnnulus(real innerRadius, real outerRadius)
	{
		real f = Mathf.Pi * 2f * Real;
		Vector2 vector = new Vector2(Mathf.Cos(f), Mathf.Sin(f));
		innerRadius *= innerRadius;
		outerRadius *= outerRadius;
		return Mathf.Sqrt(Mathf.Lerp(innerRadius, outerRadius, Float)) * vector;
	}

	/// <summary>
	/// Calculates a random position inside the given circle2F.
	/// </summary>
	/// <param name="circle2F">The circle2F in which to generate a random position.</param>
	/// <returns>A random position inside the specified circle2F.</returns>
	public static Vector2 InsideCircle(Circle circle2F)
	{
		var radius = InsideUnitCircle * circle2F.Radius;
		return circle2F.Position + radius;
	}

	/// <summary>
	/// Calculates a random position on the circumference of the given circle2F.
	/// </summary>
	/// <param name="circle2F">The circle2 in which to generate a random position on the circumference.</param>
	/// <returns>A random position on the circumference of the specified circle2F.</returns>
	public static Vector2 OnCircle(Circle circle2F)
	{
		var radius = UnitVector * circle2F.Radius;
		return circle2F.Position + radius;
	}
		
	/// <summary>
	/// Calculates a random position on the circumference of the given circle2F.
	/// </summary>
	/// <param name="circle">The circle2F in which to generate a random position on the circumference.</param>
	/// <returns>A random position on the circumference of the specified circle2F.</returns>
	public static Vector2 OnCircleD(Circle circle)
	{
		var radius = UnitVector * circle.Radius;
		return circle.Position + radius;
	}

	/// <summary>
	/// Generates a random value following a Gaussian distribution.
	/// </summary>
	/// <param name="centerX">The center value of the distribution.</param>
	/// <param name="widthFactor">The width factor of the distribution.</param>
	/// <returns>A random value following a Gaussian distribution.</returns>
	public static double Gaussian(double centerX = 0f, double widthFactor = 1f)
	{
		var value = Double;
		var value2 = Double;
		return Mathf.Sqrt(-2f * Mathf.Log(value)) * Mathf.Sin(Mathf.Pi * 2d * value2) * widthFactor + centerX;
	}
		
	/// <summary>
	/// Generates a random value following a Gaussian distribution.
	/// </summary>
	/// <param name="centerX">The center value of the distribution.</param>
	/// <param name="widthFactor">The width factor of the distribution.</param>
	/// <returns>A random value following a Gaussian distribution.</returns>
	public static float GaussianF(float centerX = 0f, float widthFactor = 1f)
	{
		var value = Float;
		var value2 = Float;
		return (float)(Mathf.Sqrt(-2f * Mathf.Log(value)) * Mathf.Sin(Mathf.Pi * 2f * value2) * widthFactor + centerX);
	}

	/// <summary>
	/// Generates a random value following an asymmetric Gaussian distribution.
	/// </summary>
	/// <param name="centerX">The center value of the distribution.</param>
	/// <param name="lowerWidthFactor">The width factor for values less than or equal to zero.</param>
	/// <param name="upperWidthFactor">The width factor for values greater than zero.</param>
	/// <returns>A random value following an asymmetric Gaussian distribution.</returns>
	public static double GaussianAsymmetric(double centerX = 0f, double lowerWidthFactor = 1f, double upperWidthFactor = 1f)
	{
		double value = Float;
		double value2 = Float;
		double num = Mathf.Sqrt(-2f * Mathf.Log(value)) * Mathf.Sin(Mathf.Pi * 2f * value2);
		if (num <= 0f)
		{
			return num * lowerWidthFactor + centerX;
		}
		return num * upperWidthFactor + centerX;
	}

	/// <summary>
	/// Generates a random integer value within the specified range.
	/// </summary>
	/// <param name="min">The inclusive minimum value of the range.</param>
	/// <param name="max">The exclusive maximum value of the range.</param>
	/// <returns>A random integer value within the specified range.</returns>
	public static int Range(int min, int max)
	{
		if (max <= min)
		{
			return min;
		}
		return min + Mathf.Abs(Int % (max - min));
	}

	/// <summary>
	/// Generates a random integer value within the specified range.
	/// </summary>
	/// <param name="max">The exclusive maximum value of the range.</param>
	/// <returns>A random integer value within the specified range.</returns>
	public static int Range(int max)
	{
		var min = 0;
		if (max <= min)
		{
			return min;
		}
		return Mathf.Abs(Int % (max));
	}

	/// <summary>
	/// Generates a random integer value within the specified range (inclusive).
	/// </summary>
	/// <param name="min">The inclusive minimum value of the range.</param>
	/// <param name="max">The inclusive maximum value of the range.</param>
	/// <returns>A random integer value within the specified range (inclusive).</returns>
	public static int RangeInclusive(int min, int max)
	{
		if (max <= min)
		{
			return min;
		}
		return Range(min, max + 1);
	}

	/// <summary>
	/// Generates a random integer value within the specified range (inclusive).
	/// </summary>
	/// <param name="max">The inclusive maximum value of the range.</param>
	/// <returns>A random integer value within the specified range (inclusive).</returns>
	public static int RangeInclusive(int max)
	{
		var min = 0;
		if (max <= min)
		{
			return min;
		}
		return Range(min, max + 1);
	}

	/// <summary>
	/// Generates a random float value within the specified range.
	/// </summary>
	/// <param name="min">The inclusive minimum value of the range.</param>
	/// <param name="max">The exclusive maximum value of the range.</param>
	/// <returns>A random float value within the specified range.</returns>
	public static float Range(float min, float max)
	{
		if (max <= min)
		{
			return min;
		}
		return Float * (max - min) + min;
	}

	/// <summary>
	/// Generates a random float value within the specified range.
	/// </summary>
	/// <param name="max">The exclusive maximum value of the range.</param>
	/// <returns>A random float value within the specified range.</returns>
	public static float Range(float max)
	{
		var min = 0;
		if (max <= min)
		{
			return min;
		}
		return Float * (max - min) + min;
	}

	/// <summary>
	/// Generates a random double value within the specified range.
	/// </summary>
	/// <param name="min">The inclusive minimum value of the range.</param>
	/// <param name="max">The exclusive maximum value of the range.</param>
	/// <returns>A random float value within the specified range.</returns>
	public static double Range(double min, double max)
	{
		if (max <= min)
		{
			return min;
		}
		return Float * (max - min) + min;
	}

	/// <summary>
	/// Generates a random double value within the specified range.
	/// </summary>
	/// <param name="max">The exclusive maximum value of the range.</param>
	/// <returns>A random float value within the specified range.</returns>
	public static double Range(double max)
	{
		var min = 0;
		if (max <= min)
		{
			return min;
		}
		return Float * (max - min) + min;
	}

	/// <summary>
	/// Determines whether an event with the specified chance occurs.
	/// </summary>
	/// <param name="chance">The probability of the event occurring, ranging from 0 to 1.</param>
	/// <returns>True if the event occurs, false otherwise.</returns>
	public static bool Chance(float chance)
	{
		if (chance <= 0f)
		{
			return false;
		}
		if (chance >= 1f)
		{
			return true;
		}
		return Float < chance;
	}

	/// <summary>
	/// Determines whether an event with the specified chance occurs.
	/// </summary>
	/// <param name="chance">The probability of the event occurring, ranging from 0 to 1.</param>
	/// <returns>True if the event occurs, false otherwise.</returns>
	public static bool Chance(double chance)
	{
		if (chance <= 0)
		{
			return false;
		}
		if (chance >= 1)
		{
			return true;
		}

		return Double < chance;
	}

	/// <summary>
	/// Saves the current state of the pseudo-random number generator to the state stack.
	/// </summary>
	public static void PushState()
	{
		_stateStack.Push(StateCompressed);
	}

	/// <summary>
	/// Saves the current state of the pseudo-random number generator to the state stack
	/// and sets a new seed for the generator.
	/// </summary>
	/// <param name="replacementSeed">The new seed to set for the generator.</param>
	public static void PushState(int replacementSeed)
	{
		PushState();
		Seed = replacementSeed;
	}

	/// <summary>
	/// Restores the previously saved state of the pseudo-random number generator from the state stack.
	/// </summary>
	public static void PopState()
	{
		StateCompressed = _stateStack.Pop();
	}

	/// <summary>
	/// Возвращает случайный элемент списка
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static T GetRandom<T>(this IEnumerable<T> sequence)
	{
		if (sequence == null)
		{
			throw new ArgumentNullException();
		}

		if (!sequence.Any())
		{
			throw new ArgumentException("The sequence is empty.");
		}

		//optimization for ICollection<T>
		if (sequence is ICollection<T>)
		{
			ICollection<T> col = (ICollection<T>)sequence;
			return col.ElementAt(Range(col.Count));
		}

		int count = 1;
		T selected = default(T);

		foreach (T element in sequence)
		{
			if (Range(count++) == 0)
			{
				//Select the current element with 1/count probability
				selected = element;
			}
		}

		return selected;
	}
}



/// <summary>
/// A static class containing methods for performing MurmurHash calculations.
/// </summary>
public static class MurmurHash
{
	// Constants used in the MurmurHash algorithm
	private const uint Const1 = 3432918353u;
	private const uint Const2 = 461845907u;
	private const uint Const3 = 3864292196u;
	private const uint Const4Mix = 2246822507u;
	private const uint Const5Mix = 3266489909u;
	private const uint Const6StreamPosition = 2834544218u;

	/// <summary>
	/// Calculates the MurmurHash hash code for a given seed and input value.
	/// </summary>
	/// <param name="seed">The seed value for the hash calculation.</param>
	/// <param name="input">The input value to be hashed.</param>
	/// <returns>The 32-bit signed integer representing the MurmurHash hash code.</returns>
	public static int GetInt(uint seed, uint input)
	{
		// Perform MurmurHash algorithm
		uint num = input;
		num *= Const1;
		num = (num << 15) | (num >> 17);
		num *= Const2;
		uint num2 = seed;
		num2 ^= num;
		num2 = (num2 << 13) | (num2 >> 19);
		num2 = num2 * 5 + Const3;
		num2 ^= Const6StreamPosition;
		num2 ^= num2 >> 16;
		num2 *= Const4Mix;
		num2 ^= num2 >> 13;
		num2 *= Const5Mix;

		// Return the result as a 32-bit signed integer
		return (int)(num2 ^ (num2 >> 16));
	}
}
