using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Caching
{
	public interface ICacheProvider
	{
		Task<T> GetAsync<T>(string key, JsonSerializerSettings jsonSerializerSettings = null);

		Task SetAsync<T>(string key, T item, JsonSerializerSettings jsonSerializerSettings = null);

		Task SetAsync<T>(string key, T item, TimeSpan expiration, bool isSliding, JsonSerializerSettings jsonSerializerSettings = null);

		Task SetAsync<T>(string key, T item, DateTimeOffset absoluteExpiration, JsonSerializerSettings jsonSerializerSettings = null);

		Task RemoveAsync<T>(string key);

        Task<bool> KeyExists<T>(string key);

        Task CreateListAsync<T>(IEnumerable<T> items, string key);

        Task<IEnumerable<T>> ListRangeAsync<T>(string key, int start, int stop);

        Task KeyDeleteAsync<T>(string key);
    }
}
