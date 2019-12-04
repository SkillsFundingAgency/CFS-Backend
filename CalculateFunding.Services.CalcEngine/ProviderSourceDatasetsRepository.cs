using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;
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

        public async Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(IEnumerable<string> providerIds,
            IEnumerable<string> dataRelationshipIds)
        {
            if (providerIds.IsNullOrEmpty() || dataRelationshipIds.IsNullOrEmpty())
            {
                return Enumerable.Empty<ProviderSourceDataset>();
            }

            ConcurrentBag<ProviderSourceDataset> results = new ConcurrentBag<ProviderSourceDataset>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(_engineSettings.GetProviderSourceDatasetsDegreeOfParallelism);

            foreach (string dataRelationshipId in dataRelationshipIds)
            {
                Guid cachedVersionKey = await _datasetVersionKeyProvider.GetProviderSourceDatasetVersionKey(dataRelationshipId);

                bool versionKeyExisted = cachedVersionKey != default;

                if (!versionKeyExisted)
                {
                    cachedVersionKey = Guid.NewGuid();

                    await _datasetVersionKeyProvider.AddOrUpdateProviderSourceDatasetVersionKey(dataRelationshipId, cachedVersionKey);
                }

                foreach (string providerId in providerIds)
                {
                    if (versionKeyExisted && TryGetProviderDataSourceFromFileSystem(dataRelationshipId, providerId, cachedVersionKey, results))
                    {
                        continue;
                    }

                    await throttler.WaitAsync();
                    
                    allTasks.Add(
                        Task.Run(async () =>
                        {
                            try
                            {
                                CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
                                {
                                    QueryText = @"SELECT     *
                                            FROM    Root r 
                                            WHERE   r.documentType = @DocumentType
                                                    AND r.content.dataRelationship.id = @DataRelationshipId 
                                                    AND r.deleted = false",
                                    Parameters = new[]
                                    {
                                        new CosmosDbQueryParameter("@DocumentType", nameof(ProviderSourceDataset)),
                                        new CosmosDbQueryParameter("@DataRelationshipId", dataRelationshipId)
                                    }
                                };

                                ProviderSourceDataset providerSourceDatasetResult =
                                    (await _cosmosRepository.QueryPartitionedEntity<ProviderSourceDataset>(cosmosDbQuery, partitionKey: providerId, maxItemCount: 1)).SingleOrDefault();

                                if (providerSourceDatasetResult != null)
                                {
                                    results.Add(providerSourceDatasetResult);

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

            return results.AsEnumerable();
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
            ConcurrentBag<ProviderSourceDataset> datasets)
        {
            ProviderSourceDatasetFileSystemCacheKey fileSystemCacheKey = new ProviderSourceDatasetFileSystemCacheKey(relationshipId, providerId, versionKey);

            if (!_fileSystemCache.Exists(fileSystemCacheKey))
            {
                return false;
            }

            using (Stream providerSourceDatasetStream = _fileSystemCache.Get(fileSystemCacheKey))
            {
                datasets.Add(providerSourceDatasetStream.AsPoco<ProviderSourceDataset>());
            }

            return true;
        }
    }
}