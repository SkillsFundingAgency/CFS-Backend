using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Services;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class PublishedProviderVersionInMemoryRepository : IVersionRepository<PublishedProviderVersion>
    {
        public IEnumerable<PublishedProviderVersion> UpsertedPublishedProviderVersions =>
            _cosmosRepository.PublishedProviderVersions.Values.SelectMany(_ => _.Values);

        private readonly VersionRepository<PublishedProviderVersion> _realImplementation;
        private readonly InMemoryCosmosRepository _cosmosRepository;

        public PublishedProviderVersionInMemoryRepository(InMemoryCosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;

            _realImplementation = new VersionRepository<PublishedProviderVersion>(_cosmosRepository, new NewVersionBuilderFactory<PublishedProviderVersion>());
        }

        public Task<PublishedProviderVersion> CreateVersion(PublishedProviderVersion newVersion, PublishedProviderVersion currentVersion = null, string partitionKey = null, bool incrementFromCurrentVersion = false)
        {
            return _realImplementation.CreateVersion(newVersion, currentVersion, partitionKey, incrementFromCurrentVersion);
        }

        public Task<int> GetNextVersionNumber(PublishedProviderVersion version = null, int currentVersion = 0, string partitionKeyId = null, bool incrementFromCurrentVersion = false)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedProviderVersion> GetVersion(string entityId, int version)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PublishedProviderVersion>> GetVersions(string entityId, string partitionKeyId = null)
        {
            throw new NotImplementedException();
        }

        public async Task<HttpStatusCode> SaveVersion(PublishedProviderVersion newVersion)
        {
            await _cosmosRepository.UpsertAsync(newVersion);

            return HttpStatusCode.OK;
        }

        public async Task SaveVersion(PublishedProviderVersion newVersion, string partitionKey)
        {
            await _cosmosRepository.UpsertAsync(newVersion, partitionKey);
        }

        public Task SaveVersions(IEnumerable<PublishedProviderVersion> newVersions, int maxDegreesOfParallelism = 30)
        {
            throw new NotImplementedException();
        }

        public async Task SaveVersions(IEnumerable<KeyValuePair<string, PublishedProviderVersion>> newVersions, int maxDegreesOfParallelism = 30)
        {
            if (newVersions.AnyWithNullCheck())
            {
                foreach (KeyValuePair<string, PublishedProviderVersion> version in newVersions)
                {
                    await _cosmosRepository.UpsertAsync(version.Value);
                }
            }
        }

        public Task DeleteVersions(IEnumerable<KeyValuePair<string, PublishedProviderVersion>> newVersions, int maxDegreesOfParallelism = 30)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PublishedProviderVersion>> GetVersions(string entityId, int? offset, int? limit)
        {
            throw new NotImplementedException();
        }

        public Task<int?> GetVersionCount(string entityId)
        {
            throw new NotImplementedException();
        }
    }
}
