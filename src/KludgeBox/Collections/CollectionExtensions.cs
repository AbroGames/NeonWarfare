using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace KludgeBox.Collections;

public static class CollectionExtensions
{


	/// <summary>
	/// Splits an IEnumerable into multiple IEnumerables of a specified size.
	/// </summary>
	/// <typeparam name="T">The type of elements in the IEnumerable.</typeparam>
	/// <param name="source">The IEnumerable to split.</param>
	/// <param name="splitSize">The size of each split IEnumerable.</param>
	/// <returns>An IEnumerable of IEnumerables, each containing splitSize elements from the source.</returns>
	public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int splitSize)
	{
		using (IEnumerator<T> enumerator = source.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				yield return InnerSplit(enumerator, splitSize);
			}
		}

	}

	private static IEnumerable<T> InnerSplit<T>(IEnumerator<T> enumerator, int splitSize)
	{
		int count = 0;
		do
		{
			count++;
			yield return enumerator.Current;
		}
		while (count % splitSize != 0
		       && enumerator.MoveNext());
	}


	/// <summary>
	/// Converts a System.IO.Stream to a byte array.
	/// </summary>
	/// <param name="stream">The stream to convert.</param>
	/// <returns>A byte array containing the data from the stream.</returns>
	public static byte[] ConvertToByteArray(this Stream stream)
	{
		var streamLength = Convert.ToInt32(stream.Length);
		byte[] data = new byte[streamLength + 1];

		//convert to to a byte array
		stream.Read(data, 0, streamLength);
		stream.Close();

		return data;
	}


	/// <summary>
	/// Returns the first element of a sequence, or null if the sequence contains no elements.
	/// </summary>
	/// <typeparam name="T">The type of the elements of source.</typeparam>
	/// <param name="values">The sequence to return the first element of.</param>
	/// <returns>null if source is empty; otherwise, the first element in source.</returns>
	public static T FirstOrNull<T>(this IEnumerable<T> values) where T : class
	{
		return values.DefaultIfEmpty(null).FirstOrDefault();
	}


	/// <summary>
	/// Flattens an <see cref="IEnumerable"/> of <see cref="string"/> objects to a single string, seperated by an optional seperator and with optional head and tail.
	/// </summary>
	/// <param name="strings">The string objects to be flattened.</param>
	/// <param name="seperator">The seperator to be used between each string object.</param>
	/// <param name="head">The string to be used at the beginning of the flattened string. Defaulted to an empty string.</param>        
	/// <param name="tail">The string to be used at the end of the flattened string. Defaulted to an empty string.</param>
	/// <returns>Single string containing all elements seperated by the given seperator, with optionally a head and/or tail.</returns>
	public static string Flatten(this IEnumerable<string> strings, string seperator, string head, string tail)
	{
		// If the collection is null, or if it contains zero elements,
		// then return an empty string.
		if (strings == null || strings.Count() == 0)
			return string.Empty;

		// Build the flattened string
		var flattenedString = new StringBuilder();

		flattenedString.Append(head);
		foreach (var s in strings)
			flattenedString.AppendFormat("{0}{1}", s, seperator); // Add each element with the given seperator.
		flattenedString.Remove(flattenedString.Length - seperator.Length, seperator.Length); // Remove the last seperator
		flattenedString.Append(tail);

		// Return the flattened string
		return flattenedString.ToString();
	}

	/// <summary>
	/// Checks whether the current collection contains any elements from another collection.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collections.</typeparam>
	/// <param name="source">The current collection.</param>
	/// <param name="otherCollection">The other collection to check for common elements.</param>
	/// <returns><c>true</c> if the current collection contains any elements from the other collection; otherwise, <c>false</c>.</returns>
	public static bool HasAnyOf<T>(this IEnumerable<T> source, IEnumerable<T> otherCollection)
	{
		return source.Intersect(otherCollection).Any();
	}

	/// <summary>
	/// Checks whether the current collection contains all elements from another collection.
	/// </summary>
	/// <typeparam name="T">The type of elements in the collections.</typeparam>
	/// <param name="source">The current collection.</param>
	/// <param name="otherCollection">The other collection to check for all elements.</param>
	/// <returns><c>true</c> if the current collection contains all elements from the other collection; otherwise, <c>false</c>.</returns>
	public static bool HasAllOf<T>(this IEnumerable<T> source, IEnumerable<T> otherCollection)
	{
		return !otherCollection.Except(source).Any();
	}

	/// <summary>
	/// Converts a regular <see cref="Dictionary{TKey, TValue}"/> into a read-only <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
	/// </summary>
	/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
	/// <param name="dict">The dictionary to convert to read-only.</param>
	/// <returns>A read-only dictionary that wraps the original dictionary.</returns>
	public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this Dictionary<TKey, TValue> dict) where TKey : notnull
	{
		return new ReadOnlyDictionary<TKey, TValue>(dict);
	}

	/// <summary>
	/// Converts a regular <see cref="HashSet{T}"/> into a read-only <see cref="ReadOnlyHashSet{T}"/>.
	/// </summary>
	/// <typeparam name="TValue">The type of values in the set.</typeparam>
	/// <param name="set">The set to convert to read-only.</param>
	/// <returns>A read-only hash set that wraps the original set.</returns>
	public static ReadOnlyHashSet<TValue> AsReadOnly<TValue>(this HashSet<TValue> set) where TValue : notnull
	{
		return new ReadOnlyHashSet<TValue>(set);
	}

	/// <summary>
	/// Generates a batch of strings based on a template with numbers ranging from min to max (inclusive).
	/// </summary>
	/// <param name="template">The template string that includes a placeholder for the number, e.g., "res://Assets/Audio/Sounds/normal/impact/hit{0}.wav".</param>
	/// <param name="min">The minimum number in the batch.</param>
	/// <param name="max">The maximum number in the batch.</param>
	/// <returns>A list of strings with the placeholders replaced by numbers from min to max.</returns>
	public static List<string> BatchNumber(this string template, int min, int max)
	{
		List<string> result = new List<string>();
		for (int i = min; i <= max; i++)
		{
			string item = string.Format(template, i);
			result.Add(item);
		}
		return result;
	}
	
	/// <summary>
	/// Generates a batch of strings based on a template with numbers ranging from min to max (inclusive).<br/>
	/// Numbers will be placed just before the file extension.
	/// </summary>
	/// <param name="path">The template path to which numbers will be appended.</param>
	/// <param name="min">The minimum number to include in the batch.</param>
	/// <param name="max">The maximum number to include in the batch.</param>
	/// <returns>A list of strings representing the generated batch with appended numbers.</returns>
	public static List<string> BatchNumberAuto(this string path, int min, int max)
	{
		List<string> result = new List<string>();

		var extension = Path.GetExtension(path);
		var pathWithoutExtension = path.Substring(0, path.Length - extension.Length);
		
		for (int i = min; i <= max; i++)
		{
			string item = $"{pathWithoutExtension}{i}{extension}";
			result.Add(item);
		}
		return result;
	}
}