using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Caching
{
	public interface ICacheProvider
	{
		Task<T> GetAsync<T>(string key);

		Task SetAsync<T>(string key, T item);

		Task SetAsync<T>(string key, T item, TimeSpan expiration, bool isSliding);

		Task SetAsync<T>(string key, T item, DateTimeOffset absoluteExpiration);

		Task RemoveAsync<T>(string key);

        Task<bool> KeyExists<T>(string key);
    }
}
