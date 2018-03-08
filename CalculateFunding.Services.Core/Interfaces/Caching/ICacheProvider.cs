using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Caching
{
	public interface ICacheProvider
	{
		Task<T> GetAsync<T>(string key);

		Task SetAsync(string key, object item);

		Task SetAsync(string key, object item, TimeSpan expiration, bool isSliding);

		Task SetAsync(string key, object item, DateTimeOffset absoluteExpiration);

		Task RemoveAsync<T>(string key);
	}
}
