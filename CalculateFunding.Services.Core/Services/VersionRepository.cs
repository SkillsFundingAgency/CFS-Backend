using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Cosmos.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;

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

        public async Task<T> CreateVersion(T newVersion, T currentVersion = null, string partitionKey = null, bool incrementFromCurrentVersion = false)
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
                newVersion.Version = await GetNextVersionNumber(newVersion, currentVersion.Version, partitionKey, incrementFromCurrentVersion);

                if (newVersion.PublishStatus == PublishStatus.Approved && (currentVersion.PublishStatus == PublishStatus.Draft || currentVersion.PublishStatus == PublishStatus.Updated))
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
                            {
                                break;
                            }
                            else
                            {
                                newVersion.PublishStatus = PublishStatus.Updated;
                            }

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

            if (version < 1)
            {
                throw new ArgumentException("Invalid version number was supplied", nameof(version));
            }

            IQueryable<T> versions = _cosmosRepository.Query<T>().Where(m => m.EntityId == entityId && m.Version == version);

            return Task.FromResult(versions.AsEnumerable().FirstOrDefault());
        }

        public Task<int> GetNextVersionNumber(T version = null, int currentVersion = 0, string partitionKeyId = null, bool incrementFromCurrentVersion = false)
        {
            Guard.ArgumentNotNull(version, nameof(version));

            if(incrementFromCurrentVersion)
            {
                return Task.FromResult(currentVersion + 1);
            }

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

        public async Task<IEnumerable<T>> GetVersions(string entityId, string partitionKeyId = null)
        {
            Guard.IsNullOrWhiteSpace(entityId, nameof(entityId));

            IEnumerable<T> versions = Enumerable.Empty<T>();

            if (partitionKeyId == null)
            {
                versions = _cosmosRepository.Query<T>().Where(m => m.EntityId == entityId);
            }
            else
            {
                string query = $"select * from Root c where c.documentType = '{typeof(T).Name}' and c.deleted = false and c.content.entityId = '{entityId}'";

                versions = await _cosmosRepository.QueryPartitionedEntity<T>(query, partitionEntityId: partitionKeyId);
            }

            return versions;
        }

    }
}
