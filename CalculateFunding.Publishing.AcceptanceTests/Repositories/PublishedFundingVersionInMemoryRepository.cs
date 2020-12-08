using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Services;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class PublishedFundingVersionInMemoryRepository : IVersionRepository<PublishedFundingVersion>
    {
        private readonly InMemoryCosmosRepository _cosmosRepository;
        private readonly VersionRepository<PublishedFundingVersion> _realImplementation;

        public PublishedFundingVersionInMemoryRepository(InMemoryCosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
            _realImplementation = new VersionRepository<PublishedFundingVersion>(_cosmosRepository);
        }

        public Task<PublishedFundingVersion> CreateVersion(PublishedFundingVersion newVersion, PublishedFundingVersion currentVersion = null, string partitionKey = null, bool incrementFromCurrentVersion = false)
        {
            return _realImplementation.CreateVersion(newVersion, currentVersion, partitionKey, incrementFromCurrentVersion);
        }

        public Task DeleteVersions(IEnumerable<KeyValuePair<string, PublishedFundingVersion>> newVersions, int maxDegreesOfParallelism = 30)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetNextVersionNumber(PublishedFundingVersion version = null, int currentVersion = 0, string partitionKeyId = null, bool incrementFromCurrentVersion = false)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedFundingVersion> GetVersion(string entityId, int version)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PublishedFundingVersion>> GetVersions(string entityId, string partitionKeyId = null)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public async Task<HttpStatusCode> SaveVersion(PublishedFundingVersion newVersion)
        {
            await _cosmosRepository.UpsertAsync(newVersion);

            return HttpStatusCode.OK;
        }

        public async Task SaveVersion(PublishedFundingVersion newVersion, string partitionKey)
        {
            await _cosmosRepository.UpsertAsync(newVersion, partitionKey);
        }

        public Task SaveVersions(IEnumerable<PublishedFundingVersion> newVersions, int maxDegreesOfParallelism = 30)
        {
            throw new NotImplementedException();
        }

        public Task SaveVersions(IEnumerable<KeyValuePair<string, PublishedFundingVersion>> newVersions, int maxDegreesOfParallelism = 30)
        {
            throw new NotImplementedException();
        }
    }
}
