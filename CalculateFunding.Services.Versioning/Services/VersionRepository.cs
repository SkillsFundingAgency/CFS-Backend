using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Versioning.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Versioning.Services
{
    public class VersionRepository<T> : IVersionRepository<T> where T : VersionedItem
    {
        private readonly CosmosRepository _cosmosRepository;

        public VersionRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task SaveVersion(T newVersion, T currentVersion = null)
        {
            if (currentVersion == null)
            {
                newVersion.Version = 1;

                newVersion.PublishStatus = PublishStatus.Draft;
            }
            else
            {
                newVersion.Version = await GetNextVersionNumber(newVersion);

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
           
            newVersion.Date = DateTimeOffset.Now.ToLocalTime();

            await _cosmosRepository.CreateAsync<T>(newVersion);

        }

        public Task<IEnumerable<T>> GetVersions(string entityId)
        {
            return GetVersionsByEntityId(entityId);
        }

        private async Task<int> GetNextVersionNumber(T version)
        {
            string entityId = version.EntityId;

            IEnumerable<T> versions = await GetVersionsByEntityId(entityId);

            return versions.Max(m => m.Version) + 1;
        }

        private Task<IEnumerable<T>> GetVersionsByEntityId(string entityId)
        {
            IQueryable<T> versions = _cosmosRepository.Query<T>().Where(m => m.EntityId == entityId);

            return Task.FromResult(versions.AsEnumerable());
        }

    }
}
