using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Core.Interfaces.Caching
{
    public interface ICacheProvider
    {
        Task<(bool Ok, string Message)> IsHealthOk();

        Task<T> GetAsync<T>(string key, JsonSerializerSettings jsonSerializerSettings = null);

        Task SetAsync<T>(string key, T item, JsonSerializerSettings jsonSerializerSettings = null);

        Task SetAsync<T>(string key, T item, TimeSpan expiration, bool isSliding, JsonSerializerSettings jsonSerializerSettings = null);

        Task SetAsync<T>(string key, T item, DateTimeOffset absoluteExpiration, JsonSerializerSettings jsonSerializerSettings = null);

        Task RemoveAsync<T>(string key);

        Task<bool> KeyExists<T>(string key);

        Task CreateListAsync<T>(IEnumerable<T> items, string key);

        Task<IEnumerable<T>> ListRangeAsync<T>(string key, int start, int stop);

        Task KeyDeleteAsync<T>(string key);

        Task<long> ListLengthAsync<T>(string key);

        /// <summary>
        /// Set Hash Value
        /// </summary>
        /// <param name="cacheKey">Cache Key</param>
        /// <param name="hashKey">Hash set key</param>
        /// <param name="value">Contents of hash key</param>
        /// <returns></returns>
        Task<bool> SetHashValue<T>(string cacheKey, string hashKey, T value);

        /// <summary>
        /// Removes an entire hash set (keys and values) given the cache key
        /// </summary>
        /// <param name="cacheKey">Cache Key</param>
        /// <returns></returns>
        Task<bool> DeleteHashSet(string cacheKey);

        /// <summary>
        /// Gets hash value from hash stored at cacheKey
        /// </summary>
        /// <param name="cacheKey">Cache key</param>
        /// <param name="hashKey">Hash key</param>
        /// <returns>Object stored in cache or default(T) if not found</returns>
        Task<T> GetHashValue<T>(string cacheKey, string hashKey);

        /// <summary>
        /// Set absolute date of expiry for Hash Set
        /// </summary>
        /// <param name="cacheKey">Cache Key</param>
        /// <param name="expiry">Expiry date, or null for no expiry</param>
        /// <returns></returns>
        Task<bool> SetHashExpiry(string cacheKey, DateTime? expiry);

        /// <summary>
        /// Does the hash set exist in case (whole hashset, not key within)
        /// </summary>
        /// <param name="cacheKey">Cache Key</param>
        /// <returns></returns>
        Task<bool> HashSetExists(string cacheKey);
    }
}
