﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace BizHawk.Common
{
	public class SortedList<T> : ICollection<T>
		where T : IComparable<T>
	{
		protected readonly List<T> _list;

		public virtual int Count => _list.Count;

		public virtual bool IsReadOnly { get; } = false;

		public SortedList() => _list = new List<T>();

		public SortedList(IEnumerable<T> collection)
		{
			_list = new List<T>(collection);
			_list.Sort();
		}

		public virtual T this[int index] => _list[index];

		public virtual void Add(T item)
		{
			var i = _list.BinarySearch(item);
			_list.Insert(i < 0 ? ~i : i, item);
		}

		public virtual int BinarySearch(T item) => _list.BinarySearch(item);

		public virtual void Clear() => _list.Clear();

		public virtual bool Contains(T item) => !(_list.BinarySearch(item) < 0); // can't use `!= -1`, BinarySearch can return multiple negative values

		public virtual void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

		public virtual IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

		public virtual int IndexOf(T item)
		{
			var i = _list.BinarySearch(item);
			return i < 0 ? -1 : i;
		}

		public virtual bool Remove(T item)
		{
#if true
			var i = _list.BinarySearch(item);
			if (i < 0) return false;
			_list.RemoveAt(i);
			return true;
#else //TODO is this any slower?
			return _list.Remove(item);
#endif
		}


		public virtual int RemoveAll(Predicate<T> match) => _list.RemoveAll(match);

		public virtual void RemoveAt(int index) => _list.RemoveAt(index);

		/// <summary>Remove all items after the specific item (but not the given item).</summary>
		public virtual void RemoveAfter(T item)
		{
			var startIndex = _list.BinarySearch(item);
			if (startIndex < 0)
			{
				// If BinarySearch doesn't find the item,
				// it returns the bitwise complement of the index of the next element
				// that is larger than item
				startIndex = ~startIndex;
			}
			else
			{
				// All items *after* the item
				startIndex = startIndex + 1;
			}
			if (startIndex < _list.Count)
			{
				_list.RemoveRange(startIndex, _list.Count - startIndex);
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>A dictionary whose index getter creates an entry if the requested key isn't part of the collection, making it always safe to use the returned value. The new entry's value will be the result of the default constructor of <typeparamref name="TValue"/>.</summary>
	[Serializable]
	public class WorkingDictionary<TKey, TValue> : Dictionary<TKey, TValue>
		where TKey : notnull
		where TValue : new()
	{
		public WorkingDictionary() {}

		protected WorkingDictionary(SerializationInfo info, StreamingContext context) : base(info, context) {}

		[property: MaybeNull]
		public new TValue this[TKey key]
		{
			get => TryGetValue(key, out var temp)
				? temp
				: base[key] = new TValue();
			set => base[key] = value;
		}
	}
}
