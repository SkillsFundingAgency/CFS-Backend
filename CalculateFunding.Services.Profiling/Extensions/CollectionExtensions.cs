using System.Collections.Generic;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Services.Profiling.Extensions
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> parent, IEnumerable<T> collectionToAdd)
        {
            Guard.ArgumentNotNull(parent, nameof(parent));
            Guard.ArgumentNotNull(collectionToAdd, nameof(collectionToAdd));

            foreach (T item in collectionToAdd)
            {
                parent.Add(item);
            }
        }
    }
}