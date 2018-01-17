using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace System.Linq
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	static public class IEnumerableExtensions
    {
		static public bool AnyWithNullCheck<T>(this IEnumerable<T> enumerable)
		{
			if (enumerable == null)
				return false;

			return enumerable.Any();
		}

		static public bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
		{
			return !enumerable.AnyWithNullCheck();
		}

		static public T[] ToArraySafe<T>(this IEnumerable<T> enumerable)
		{
			return (enumerable ?? Enumerable.Empty<T>()).ToArray();
		}

		static public bool EqualTo<T>(this IEnumerable<T> enumerable, IEnumerable<T> other)
		{
			return enumerable.OrderBy(m => m).SequenceEqual(other.OrderBy(m => m));
		}

		static public IEnumerable<IEnumerable<TSource>> Partition<TSource>(this IEnumerable<TSource> source, int size)
		{
			var batch = new List<TSource>();
			foreach (var item in source)
			{
				batch.Add(item);
				if (batch.Count != size)
					continue;

				yield return batch;
				batch = new List<TSource>();
			}

			if (batch.Any())
				yield return batch;
		}

		static public bool ContainsDuplicates<T>(this IEnumerable<T> enumerable)
		{
			if (enumerable.IsNullOrEmpty())
				return false;

			var knownKeys = new HashSet<T>();

			return enumerable.Any(item => !knownKeys.Add(item));
		}

		static public IEnumerable<T> Flatten<T>(this IEnumerable<T> enumerable, Func<T, IEnumerable<T>> func)
		{
			return enumerable.SelectMany(c => func(c).Flatten(func)).Concat(enumerable);
		}

		static public IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			var keys = new HashSet<TKey>();
			foreach (TSource element in source)
			{
				if (keys.Add(keySelector(element)))
				{
					yield return element;
				}
			}
		}
    }
}
