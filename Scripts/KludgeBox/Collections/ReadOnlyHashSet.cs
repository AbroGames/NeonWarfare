using System;
using System.Collections;
using System.Collections.Generic;

namespace NeonWarfare.Scripts.KludgeBox.Collections;

public class ReadOnlyHashSet<T> : IReadOnlyCollection<T>
{
	private readonly HashSet<T> _hashSet;

	public ReadOnlyHashSet(HashSet<T> hashSet)
	{
		_hashSet = hashSet ?? throw new ArgumentNullException(nameof(hashSet));
	}

	public int Count => _hashSet.Count;

	public IEnumerator<T> GetEnumerator()
	{
		return _hashSet.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _hashSet.GetEnumerator();
	}
}
