using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;

namespace CalculateFunding.Services.Core.Caching
{
    public class ProviderSourceDatasetVersionKeyProvider : IProviderSourceDatasetVersionKeyProvider
    {
        private readonly ICacheProvider _cacheProvider;

        public const string CacheKey = "ProviderDatasetVersion";

        public ProviderSourceDatasetVersionKeyProvider(ICacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        public async Task<Guid> GetProviderSourceDatasetVersionKey(string dataRelationshipId)
        {
            return await _cacheProvider.GetAsync<Guid>(CacheKeyFor(dataRelationshipId));
        }

        public async Task AddOrUpdateProviderSourceDatasetVersionKey(string dataRelationshipId, Guid key)
        {
            await _cacheProvider.SetAsync(CacheKeyFor(dataRelationshipId), key);
        }

        private static string CacheKeyFor(string relationshipId) => $"{CacheKey}:{relationshipId}";
    }
}