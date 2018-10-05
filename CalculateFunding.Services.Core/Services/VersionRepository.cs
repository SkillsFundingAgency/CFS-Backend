using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Cosmos.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Services
{
    public class VersionRepository<T> : IVersionRepository<T> where T : VersionedItem
    {
        private readonly ICosmosRepository _cosmosRepository;

        public VersionRepository(ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
        }

        public Task SaveVersion(T newVersion)
        {
            Guard.ArgumentNotNull(newVersion, nameof(newVersion));
            return _cosmosRepository.CreateAsync<T>(newVersion);
        }

        public Task SaveVersions(IEnumerable<T> newVersions, int maxDegreesOfParallelism = 30)
        {
            Guard.ArgumentNotNull(newVersions, nameof(newVersions));
            return _cosmosRepository.BulkCreateAsync<T>(newVersions.ToList(), degreeOfParallelism: maxDegreesOfParallelism);
        }

        public Task SaveVersions(IEnumerable<KeyValuePair<string, T>> newVersions, int maxDegreesOfParallelism = 30)
        {
            Guard.ArgumentNotNull(newVersions, nameof(newVersions));
            return _cosmosRepository.BulkCreateAsync<T>(newVersions.ToList(), degreeOfParallelism: maxDegreesOfParallelism);
        }

        public T CreateVersion(T newVersion, T currentVersion = null)
        {
            Guard.ArgumentNotNull(newVersion, nameof(newVersion));

            newVersion.Date = DateTimeOffset.Now.ToLocalTime();

            if (currentVersion == null)
            {
                newVersion.Version = 1;

                newVersion.PublishStatus = PublishStatus.Draft;
            }
            else
            {
                newVersion.Version = GetNextVersionNumber(newVersion).Result;

                if (newVersion.PublishStatus == PublishStatus.Approved && currentVersion.PublishStatus == PublishStatus.Draft)
                {
                    return newVersion;
                }
                else
                {
                    switch (currentVersion.PublishStatus)
                    {
                        case PublishStatus.Draft:
                            newVersion.PublishStatus = PublishStatus.Draft;
                            break;

                        case PublishStatus.Approved:
                            if (newVersion.PublishStatus == PublishStatus.Draft)
                                break;
                            else
                                newVersion.PublishStatus = PublishStatus.Updated;
                            break;

                        default:
                            newVersion.PublishStatus = PublishStatus.Updated;
                            break;
                    }
                }
            }
 
            return newVersion;
        }

      
        public Task<T> GetVersion(string entityId, int version)
        {
            Guard.IsNullOrWhiteSpace(entityId, nameof(entityId));

            if(version < 1)
            {
                throw new ArgumentException("Invalid version number was supplied", nameof(version));
            }

            IQueryable<T> versions = _cosmosRepository.Query<T>().Where(m => m.EntityId == entityId && m.Version == version);

            return Task.FromResult(versions.AsEnumerable().FirstOrDefault());
        }

        public Task<int> GetNextVersionNumber(T version, string partitionKeyId = null)
        {
            try
            {
                Guard.ArgumentNotNull(version, nameof(version));

                string entityId = version.EntityId;

                string query = $"SELECT VALUE Max(c.content.version) FROM c where c.content.entityId = \"{ entityId }\" and c.documentType = \"{ typeof(T).Name }\" and c.deleted = false";

                dynamic[] resultsArray = null;

                if (string.IsNullOrWhiteSpace(partitionKeyId))
                {
                    resultsArray = _cosmosRepository.DynamicQuery<dynamic>(query).ToArray();
                }
                else
                {
                    resultsArray = _cosmosRepository.DynamicQueryPartionedEntity<dynamic>(query, partitionKeyId).ToArray();
                }

                if (resultsArray.IsNullOrEmpty())
                {
                    return Task.FromResult(1);
                }

                int nextVersionNumber = (int)resultsArray[0] + 1;

                return Task.FromResult(nextVersionNumber);
            }
            catch(Exception ex)
            {
                return Task.FromResult(1);
            }
        }

        public async Task<IEnumerable<T>> GetVersions(string sql = null, string partitionKeyId = null)
        {
            IEnumerable<T> versions = Enumerable.Empty<T>();

            if (partitionKeyId == null)
            {
                versions = _cosmosRepository.Query<T>(sql).AsEnumerable();
            }
            else
            {

                versions = await _cosmosRepository.QueryPartitionedEntity<T>(sql, partitionEntityId: partitionKeyId);
            }

            return versions;
        }

    }
}
