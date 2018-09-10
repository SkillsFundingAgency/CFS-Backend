using System;
using CalculateFunding.Services.Core.Interfaces.Caching;

namespace CalculateFunding.Services.Core.Helpers
{
	public static class CacheHelper
	{
		public static async void UpdateCacheForItem<T>(string key, T item, ICacheProvider cacheProvider, int cacheExpiryInHours = 6)
		{

			await cacheProvider.SetAsync(key, item, TimeSpan.FromHours(cacheExpiryInHours), false);

		}


	}
}
