using System.Collections.Generic;
using System.ComponentModel;

namespace System.Linq
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    static public class IEnumerableExtensions
    {
        public static bool AnyWithNullCheck<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return false;
            }

            return enumerable.Any();
        }

        public static bool AnyWithNullCheck<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            if (enumerable == null)
            {
                return false;
            }

            return enumerable.Any(predicate);
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable.AnyWithNullCheck();
        }

        static public T[] ToArraySafe<T>(this IEnumerable<T> enumerable)
        {
            return (enumerable ?? Enumerable.Empty<T>()).ToArray();
        }

        public static bool EqualTo<T>(this IEnumerable<T> enumerable, IEnumerable<T> other)
        {
            return enumerable.OrderBy(m => m).SequenceEqual(other.OrderBy(m => m));
        }

        public static IEnumerable<IEnumerable<TSource>> Partition<TSource>(this IEnumerable<TSource> source, int size)
        {
            List<TSource> batch = new List<TSource>();
            foreach (TSource item in source)
            {
                batch.Add(item);
                if (batch.Count != size)
                {
                    continue;
                }

                yield return batch;
                batch = new List<TSource>();
            }

            if (batch.Any())
            {
                yield return batch;
            }
        }

        public static bool ContainsDuplicates<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable.IsNullOrEmpty())
            {
                return false;
            }

            HashSet<T> knownKeys = new HashSet<T>();

            return enumerable.Any(item => !knownKeys.Add(item));
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> enumerable, Func<T, IEnumerable<T>> func)
        {
            return enumerable.SelectMany(c => func(c).Flatten(func)).Concat(enumerable);
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> keys = new HashSet<TKey>();
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
