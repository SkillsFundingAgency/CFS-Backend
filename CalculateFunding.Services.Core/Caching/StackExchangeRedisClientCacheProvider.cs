﻿using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Options;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Caching
{
    public class StackExchangeRedisClientCacheProvider : ICacheProvider, IDisposable
    {
        readonly RedisSettings _systemCacheSettings;
        readonly ILogger _logger;
        readonly Lazy<ConnectionMultiplexer> _connectionMultiplexer;

        public StackExchangeRedisClientCacheProvider(
            RedisSettings systemCacheSettings, ILogger logger)
        {
            _systemCacheSettings = systemCacheSettings;
            _logger = logger;

            _connectionMultiplexer = new Lazy<ConnectionMultiplexer>(() =>
            {
                var configurationOptions = ConfigurationOptions.Parse(_systemCacheSettings.CacheConnection);

                var multiplexer = ConnectionMultiplexer.Connect(configurationOptions);
                multiplexer.PreserveAsyncOrder = false;

                return multiplexer;
            });
        }

        async public Task<T> GetAsync<T>(string key)
        {
            key = GenerateCacheKey<T>(key);

            try
            {
                var database = GetDatabase();
                var cachedValue = await database.StringGetAsync(key).ConfigureAwait(false);
                if (!cachedValue.HasValue)
                {
                    return default(T);
                }

                var redisCacheValue = JsonConvert.DeserializeObject<RedisCacheValue>(cachedValue);
                if (redisCacheValue == null)
                    return default(T);

                if (redisCacheValue.SlidingExpiration.HasValue)
                    await database.KeyExpireAsync(key, redisCacheValue.SlidingExpiration.Value, CommandFlags.FireAndForget).ConfigureAwait(false);

                return (T)redisCacheValue.Value;

            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to get from cache with key: {key}");
                return default(T);
            }
        }

        public Task SetAsync(string key, object item)
        {
            var memoryCacheOptions = new MemoryCacheEntryOptions
            {
            };

            return SetAsyncImpl(key, item, memoryCacheOptions);
        }

        public Task SetAsync(string key, object item, TimeSpan expiration, bool isSliding)
        {
            var memoryCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = isSliding ? null : (TimeSpan?)expiration,
                SlidingExpiration = isSliding ? (TimeSpan?)expiration : null
            };

            return SetAsyncImpl(key, item, memoryCacheOptions);
        }

        public Task SetAsync(string key, object item, DateTimeOffset absoluteExpiration)
        {
            var memoryCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration
            };

            return SetAsyncImpl(key, item, memoryCacheOptions);
        }

        async Task SetAsyncImpl(string key, object item, MemoryCacheEntryOptions memoryCacheEntryOptions)
        {
            if (item == null)
                return;

            key = GenerateCacheKey(item.GetType(), key);

            try
            {
                
                var redisCacheValue = new RedisCacheValue
                {
                    SlidingExpiration = memoryCacheEntryOptions.SlidingExpiration,
                    Value = item
                };

                var valueToCache = JsonConvert.SerializeObject(redisCacheValue);

                var expirationTimespan = ConvertToTimeSpan(memoryCacheEntryOptions);
                if (expirationTimespan < TimeSpan.Zero)
                    expirationTimespan = TimeSpan.FromHours(72);

                var database = GetDatabase();
                await database.StringSetAsync(key, valueToCache, expirationTimespan, flags: CommandFlags.FireAndForget).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to set key: {key}");
            }
        }

        async public Task RemoveAsync<T>(string key)
        {
            key = GenerateCacheKey<T>(key);
            try
            {
                var database = GetDatabase();
                await database.KeyDeleteAsync(key, flags: CommandFlags.FireAndForget).ConfigureAwait(false);
            }
           
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to remove key: {key} from cache");
            }
        }

        static TimeSpan ConvertToTimeSpan(MemoryCacheEntryOptions memoryCacheEntryOptions)
        {
            if (!memoryCacheEntryOptions.AbsoluteExpiration.HasValue
                && !memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow.HasValue
                && !memoryCacheEntryOptions.SlidingExpiration.HasValue)
                return TimeSpan.FromHours(72);

            if (memoryCacheEntryOptions.AbsoluteExpiration.HasValue)
                return memoryCacheEntryOptions.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;

            if (memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow.HasValue)
                return memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow.Value;

            if (memoryCacheEntryOptions.SlidingExpiration.HasValue)
                return memoryCacheEntryOptions.SlidingExpiration.Value;

            return TimeSpan.FromHours(72);
        }

        static string GenerateCacheKey<T>(string key)
        {
            return GenerateCacheKey(typeof(T), key);
        }

        static string GenerateCacheKey(Type objectType, string key)
        {
            return $"{key}:{objectType.Name}".ToLower();
        }

        IDatabase GetDatabase()
        {
            var multiplexer = _connectionMultiplexer.Value;

            return multiplexer.GetDatabase();
        }

        public void Dispose()
        {
            if (_connectionMultiplexer.IsValueCreated)
                _connectionMultiplexer.Value.Dispose();
        }

        public class RedisCacheValue
        {
            public TimeSpan? SlidingExpiration { get; set; }

            public object Value { get; set; }
        }
    }
}
