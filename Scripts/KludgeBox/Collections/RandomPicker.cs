using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KludgeBox.Collections;

public class RandomPicker<T> : ICollection<T>
{
	public List<T> Values { get; set; } = new List<T>();

	public int Count => Values.Count;

	public bool IsReadOnly => false;

	public RandomPicker() { }
	public RandomPicker(params T[] list) => Values = list.ToList();
	public RandomPicker(IEnumerable<T> list) => Values = list.ToList();

	public T Pick() => Values.GetRandom();

	public void Add(T item)
	{
		Values.Add(item);
	}

	public void Clear()
	{
		Values.Clear();
	}

	public bool Contains(T item)
	{
		return Values.Contains(item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		Values.CopyTo(array, arrayIndex);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return Values.GetEnumerator();
	}

	public bool Remove(T item)
	{
		return Values.Remove(item);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return Values.GetEnumerator();
	}

	public static implicit operator T(RandomPicker<T> picker) => picker.Pick();
	public static implicit operator RandomPicker<T>(T item) => new RandomPicker<T>(item);
}