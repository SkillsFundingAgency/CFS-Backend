using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using Newtonsoft.Json;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryCacheProvider : ICacheProvider
    {
        readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();
        
        public Task<(bool Ok, string Message)> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public Task<T> GetAsync<T>(string key, JsonSerializerSettings jsonSerializerSettings = null)
        {
            return Task.FromResult((T)_cache[key]);
        }

        public Task SetAsync<T>(string key, T item, JsonSerializerSettings jsonSerializerSettings = null)
        {
            _cache[key] = item;
            
            return Task.CompletedTask;
        }

        public Task SetAsync<T>(string key, T item, TimeSpan expiration, bool isSliding, JsonSerializerSettings jsonSerializerSettings = null)
        {
            return SetAsync(key, item);
        }

        public Task SetAsync<T>(string key, T item, DateTimeOffset absoluteExpiration, JsonSerializerSettings jsonSerializerSettings = null)
        {
            return SetAsync(key, item);
        }

        public Task RemoveAsync<T>(string key)
        {
            return RemoveByPatternAsync(key);
        }

        public Task RemoveByPatternAsync(string key)
        {
            _cache.TryRemove(key, out object item);
            
            return Task.CompletedTask;
        }

        public Task<bool> KeyExists<T>(string key)
        {
            return Task.FromResult(_cache.ContainsKey(key));
        }

        public Task CreateListAsync<T>(IEnumerable<T> items, string key)
        {
            return SetAsync(key, items);
        }

        public Task<IEnumerable<T>> ListRangeAsync<T>(string key, int start, int stop)
        {
            throw new NotImplementedException();
        }

        public Task KeyDeleteAsync<T>(string key)
        {
            return RemoveByPatternAsync(key);
        }

        public Task<long> ListLengthAsync<T>(string key)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetHashValue<T>(string cacheKey, string hashKey, T value)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteHashSet(string cacheKey)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetHashValue<T>(string cacheKey, string hashKey)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetHashExpiry(string cacheKey, DateTime? expiry)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetExpiry<T>(string cacheKey, DateTime? expiry)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HashSetExists(string cacheKey)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteHashKey<T>(string cacheKey, string hashKey)
        {
            throw new NotImplementedException();
        }
    }
}