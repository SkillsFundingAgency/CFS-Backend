using System;
using CalculateFunding.Common.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.IdentityModel.Tokens;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class CollectionExtensions
    {
        public static KeyValuePair<TKey, TItem>[] ToKeyValuePairs<TKey, TItem>(this IEnumerable<TItem> items,
            Func<TItem, TKey> keyAccessor)
        {
            return items?.Select(_ => new KeyValuePair<TKey, TItem>(keyAccessor(_), _))
                .ToArray();
        }
        
        public static void AddRange<T>(this ICollection<T> parent, IEnumerable<T> collectionToAdd)
        {
            Guard.ArgumentNotNull(parent, nameof(parent));
            Guard.ArgumentNotNull(collectionToAdd, nameof(collectionToAdd));

            foreach (T item in collectionToAdd)
            {
                parent.Add(item);
            }
        }

        public static void AddOrUpdateRange<TKey, TItem>(this IDictionary<TKey, TItem> dictionary,
            IEnumerable<KeyValuePair<TKey, TItem>> itemsToAddOrUpdate)
        {
            Guard.ArgumentNotNull(dictionary, nameof(dictionary));
            Guard.ArgumentNotNull(itemsToAddOrUpdate, nameof(itemsToAddOrUpdate));

            foreach (KeyValuePair<TKey,TItem> keyValuePair in itemsToAddOrUpdate)
            {
                TKey key = keyValuePair.Key;
                
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key] = keyValuePair.Value;
                }
                else
                {
                    dictionary.Add(keyValuePair);
                }
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int index = 0;

            foreach (T item in collection)
            {
                if (predicate(item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
    
}
