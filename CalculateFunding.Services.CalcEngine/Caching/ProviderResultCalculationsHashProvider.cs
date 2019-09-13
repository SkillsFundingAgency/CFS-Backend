using System;
using System.Collections.Concurrent;
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
        
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> BatchHashContainers 
            = new ConcurrentDictionary<string, Dictionary<string, string>>(); 
        private static readonly ConcurrentDictionary<string, object> KeyLocks = new ConcurrentDictionary<string, object>(); 

        public ProviderResultCalculationsHashProvider(ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
  
            _cacheProvider = cacheProvider;
        }

        public void StartBatch(string specificationId,
            int partitionIndex,
            int partitionSize)
        {
            string specificationBatchKey = GetSpecificationResultsBatchCacheKey(partitionIndex, partitionSize, specificationId);

            lock (GetKeyLock(specificationBatchKey))
            {
                Dictionary<string, string> batchHashContainer = _cacheProvider.GetAsync<Dictionary<string, string>>(specificationBatchKey)
                    .GetAwaiter()
                    .GetResult();

                batchHashContainer = batchHashContainer ?? new Dictionary<string, string>();

                if (!BatchHashContainers.TryAdd(specificationBatchKey, batchHashContainer))
                {
                    throw new InvalidOperationException($"Batch updates for {specificationBatchKey} already in progress. Unable to start batch");
                }
            }
        }

        public void EndBatch(string specificationId,
            int partitionIndex,
            int partitionSize)
        {
            string specificationBatchKey = GetSpecificationResultsBatchCacheKey(partitionIndex, partitionSize, specificationId);

            lock (GetKeyLock(specificationBatchKey))
            {
                Dictionary<string, string> batchHashContainer = RemoveSpecificationBatchHashContainer(specificationBatchKey);
                
                CacheCalculationResultsHashes(specificationBatchKey, batchHashContainer)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        public bool TryUpdateCalculationResultHash(ProviderResult providerResult,
            int partitionIndex,
            int partitionSize)
        {
            string providerId = providerResult.Provider.Id;
            string specificationId = providerResult.SpecificationId;
            
            string specificationBatchKey = GetSpecificationResultsBatchCacheKey(partitionIndex, partitionSize, specificationId);

            lock (GetKeyLock(specificationBatchKey))
            {
                string calculationResultsJson = providerResult.CalculationResults.AsJson();
                string latestResultsHash = calculationResultsJson.ComputeSHA1Hash();

                Dictionary<string, string> providerCalculationResultHashes = GetSpecificationBatchHashContainer(specificationBatchKey);

                if (providerCalculationResultHashes.TryGetValue(providerId, out string previousResultsHash) && previousResultsHash == latestResultsHash)
                {
                    return false;
                }

                providerCalculationResultHashes[providerId] = latestResultsHash;

                return true;
            }
        }

        private static Dictionary<string, string> GetSpecificationBatchHashContainer(string specificationBatchKey)
        {
            if (!BatchHashContainers.TryGetValue(specificationBatchKey, out Dictionary<string, string> batchHashContainer))
            {
                throw new InvalidOperationException($"Batch updates for {specificationBatchKey} not yet in progress.");   
            }

            return batchHashContainer;
        }
        
        private static Dictionary<string, string> RemoveSpecificationBatchHashContainer(string specificationBatchKey)
        {
            if (!BatchHashContainers.Remove(specificationBatchKey, out Dictionary<string, string> batchHashContainer))
            {
                throw new InvalidOperationException($"Batch updates for {specificationBatchKey} not yet in progress.");   
            }

            return batchHashContainer;
        }

        private static string GetSpecificationResultsBatchCacheKey(int partitionIndex, int partitionSize, string specificationId)
        {
            return $"{CacheKeys.CalculationResults}{specificationId}:{partitionIndex}-{partitionSize}";
        }

        private async Task CacheCalculationResultsHashes(string specificationCalculationResultsCacheKey, Dictionary<string, string> providerCalculationResultHashes)
        {
            await _cacheProvider.SetAsync(specificationCalculationResultsCacheKey, providerCalculationResultHashes);
        }

        private object GetKeyLock(string batchKey)
        {
            return KeyLocks.GetOrAdd(batchKey, new object());
        }
    }
}