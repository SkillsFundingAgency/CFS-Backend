using CalculateFunding.Services.Core.Helpers;
using System.Collections.Generic;

namespace CalculateFunding.Services.Core.Extensions
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
