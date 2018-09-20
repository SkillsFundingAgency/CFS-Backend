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

        public Task SaveVersions(IEnumerable<T> newVersions)
        {
            Guard.ArgumentNotNull(newVersions, nameof(newVersions));
            return _cosmosRepository.BulkCreateAsync<T>(newVersions.ToList());
        }

        public async Task<T> CreateVersion(T newVersion, T currentVersion = null)
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
                newVersion.Version = await GetNextVersionNumber(newVersion);

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

        public Task<IEnumerable<T>> GetVersions(string entityId)
        {
            Guard.IsNullOrWhiteSpace(entityId, nameof(entityId));

            return GetVersionsByEntityId(entityId);
        }

        private Task<int> GetNextVersionNumber(T version)
        {
            Guard.ArgumentNotNull(version, nameof(version));

            string entityId = version.EntityId;

            string query = $"SELECT VALUE Max(c.content.version) FROM c where c.content.entityId = \"{ entityId }\" and c.documentType = \"{ typeof(T).Name }\" and c.deleted = false";

            dynamic[] resultsArray = _cosmosRepository.DynamicQuery<dynamic>(query).ToArray();

            if (resultsArray.IsNullOrEmpty())
            {
                return Task.FromResult(1);
            }

            int nextVersionNumber = (int)resultsArray[0] + 1;

            return Task.FromResult(nextVersionNumber);
        }

        private Task<IEnumerable<T>> GetVersionsByEntityId(string entityId)
        {
            Guard.IsNullOrWhiteSpace(entityId, nameof(entityId));

            IQueryable<T> versions = _cosmosRepository.Query<T>().Where(m => m.EntityId == entityId);

            return Task.FromResult(versions.AsEnumerable());
        }

    }
}
