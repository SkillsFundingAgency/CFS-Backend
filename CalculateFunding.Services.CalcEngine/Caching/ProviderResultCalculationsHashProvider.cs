using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.CalcEngine.Caching
{
    public class ProviderResultCalculationsHashProvider : IProviderResultCalculationsHashProvider
    {
        private readonly ICacheProvider _cacheProvider;

        public ProviderResultCalculationsHashProvider(ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _cacheProvider = cacheProvider;
        }

        public async Task<bool> TryUpdateCalculationResultHash(ProviderResult providerResult,
            int partitionIndex,
            int partitionSize)
        {
            string providerId = providerResult.Provider.Id;
            string specificationId = providerResult.SpecificationId;

            string calculationResultsJson = providerResult.CalculationResults.AsJson();
            string latestResultsHash = calculationResultsJson.ComputeSHA1Hash();

            string specificationCalculationResultsCacheKey = $"{CacheKeys.CalculationResults}{specificationId}:{partitionIndex}-{partitionSize}";

            Dictionary<string, string> providerCalculationResultHashes =
                await _cacheProvider.GetAsync<Dictionary<string, string>>(specificationCalculationResultsCacheKey);

            if (providerCalculationResultHashes == null)
            {
                await CacheCalculationResultsHashes(specificationCalculationResultsCacheKey, new Dictionary<string, string>
                {
                    {providerId, latestResultsHash}
                });

                return true;
            }

            if (providerCalculationResultHashes.TryGetValue(providerId, out string previousResultsHash) && previousResultsHash == latestResultsHash)
            {
                return false;
            }

            providerCalculationResultHashes[providerId] = latestResultsHash;

            await CacheCalculationResultsHashes(specificationCalculationResultsCacheKey, providerCalculationResultHashes);

            return true;
        }

        private async Task CacheCalculationResultsHashes(string specificationCalculationResultsCacheKey, Dictionary<string, string> providerCalculationResultHashes)
        {
            await _cacheProvider.SetAsync(specificationCalculationResultsCacheKey, providerCalculationResultHashes);
        }
    }
}