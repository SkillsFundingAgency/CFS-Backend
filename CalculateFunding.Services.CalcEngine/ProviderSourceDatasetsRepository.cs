using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;

namespace CalculateFunding.Services.CalcEngine
{
    public class ProviderSourceDatasetsRepository : IProviderSourceDatasetsRepository
    {
        private readonly IProviderSourceDatasetVersionKeyProvider _datasetVersionKeyProvider;
        private readonly IFileSystemCache _fileSystemCache;
        private readonly ICosmosRepository _cosmosRepository;
        private readonly EngineSettings _engineSettings;

        public ProviderSourceDatasetsRepository(ICosmosRepository cosmosRepository,
            EngineSettings engineSettings,
            IProviderSourceDatasetVersionKeyProvider datasetVersionKeyProvider,
            IFileSystemCache fileSystemCache)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));
            Guard.ArgumentNotNull(datasetVersionKeyProvider, nameof(datasetVersionKeyProvider));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));

            _cosmosRepository = cosmosRepository;
            _engineSettings = engineSettings;
            _datasetVersionKeyProvider = datasetVersionKeyProvider;
            _fileSystemCache = fileSystemCache;
        }

        public async Task<ProviderSourceDatasetLookupResult> GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(
            string specificationId,
            IEnumerable<string> providerIds,
            IEnumerable<string> dataRelationshipIds)
        {
            ProviderSourceDatasetLookupResult result = new ProviderSourceDatasetLookupResult();

            if (providerIds.IsNullOrEmpty() || dataRelationshipIds.IsNullOrEmpty())
            {
                result.ProviderSourceDatasets = new Dictionary<string, Dictionary<string, ProviderSourceDataset>>();
                return result;
            }

            ConcurrentDictionary<string, ConcurrentDictionary<string, ProviderSourceDataset>> results = new ConcurrentDictionary<string, ConcurrentDictionary<string, ProviderSourceDataset>>();

            foreach (string dataRelationshipId in dataRelationshipIds)
            {
                Guid cachedVersionKey = await _datasetVersionKeyProvider.GetProviderSourceDatasetVersionKey(dataRelationshipId);

                bool versionKeyExisted = cachedVersionKey != default;

                if (!versionKeyExisted)
                {
                    cachedVersionKey = Guid.NewGuid();

                    await _datasetVersionKeyProvider.AddOrUpdateProviderSourceDatasetVersionKey(dataRelationshipId, cachedVersionKey);
                }

                Console.WriteLine("Starting to lookup provider source datasets from cosmos");
                List<Task> allTasks = new List<Task>();
                SemaphoreSlim throttler = new SemaphoreSlim(_engineSettings.GetProviderSourceDatasetsDegreeOfParallelism);

                foreach (string providerId in providerIds)
                {
                    ConcurrentDictionary<string, ProviderSourceDataset> providerSourceDatasets = results.GetOrAdd(providerId, new ConcurrentDictionary<string, ProviderSourceDataset>());

                    if (versionKeyExisted && TryGetProviderDataSourceFromFileSystem(dataRelationshipId, providerId, cachedVersionKey, providerSourceDatasets))
                    {
                        continue;
                    }

                    await throttler.WaitAsync();

                    allTasks.Add(
                        Task.Run(async () =>
                        {
                            try
                            {
                                string documentKey = $"{specificationId}_{dataRelationshipId}_{providerId}";

                                DocumentEntity<ProviderSourceDataset> providerSourceDatasetDocument = await _cosmosRepository.TryReadDocumentByIdPartitionedAsync<ProviderSourceDataset>(documentKey, providerId);

                                if (providerSourceDatasetDocument != null && !providerSourceDatasetDocument.Deleted)
                                {
                                    ProviderSourceDataset providerSourceDatasetResult = providerSourceDatasetDocument.Content;

                                    results[providerId].TryAdd(dataRelationshipId, providerSourceDatasetResult);

                                    CacheProviderSourceDatasetToFileSystem(dataRelationshipId, providerId, cachedVersionKey, providerSourceDatasetResult);
                                }
                            }
                            finally
                            {
                                throttler.Release();
                            }
                        }));
                }

                await TaskHelper.WhenAllAndThrow(allTasks.ToArray());
            }

            result.ProviderSourceDatasets = results.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value));

            return result;
        }

        private void CacheProviderSourceDatasetToFileSystem(string relationshipId,
            string providerId,
            Guid? versionKey,
            ProviderSourceDataset providerSourceDataset)
        {
            ProviderSourceDatasetFileSystemCacheKey fileSystemCacheKey = new ProviderSourceDatasetFileSystemCacheKey(relationshipId, providerId, versionKey.Value);

            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamWriter streamWriter = new StreamWriter(memoryStream))
            {
                streamWriter.Write(providerSourceDataset.AsJson());
                streamWriter.Flush();

                memoryStream.Position = 0;

                _fileSystemCache.Add(fileSystemCacheKey, memoryStream, ensureFolderExists: true);
            }
        }

        private bool TryGetProviderDataSourceFromFileSystem(string relationshipId,
            string providerId,
            Guid versionKey,
            ConcurrentDictionary<string, ProviderSourceDataset> datasets)
        {
            ProviderSourceDatasetFileSystemCacheKey fileSystemCacheKey = new ProviderSourceDatasetFileSystemCacheKey(relationshipId, providerId, versionKey);

            if (!_fileSystemCache.Exists(fileSystemCacheKey))
            {
                return false;
            }

            using (Stream providerSourceDatasetStream = _fileSystemCache.Get(fileSystemCacheKey))
            {
                ProviderSourceDataset providerSourceDataset = providerSourceDatasetStream.AsPoco<ProviderSourceDataset>();

                if (providerSourceDataset == null)
                {
                    return false;
                }

                datasets.TryAdd(relationshipId, providerSourceDataset);
                return true;
            }
        }
    }
}