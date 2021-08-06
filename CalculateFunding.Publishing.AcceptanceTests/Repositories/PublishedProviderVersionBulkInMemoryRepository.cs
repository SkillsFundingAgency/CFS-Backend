using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Services;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class PublishedProviderVersionBulkInMemoryRepository : IVersionBulkRepository<PublishedProviderVersion>
    {
        private readonly ICosmosRepository _cosmosRepository;

        public PublishedProviderVersionBulkInMemoryRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public async Task<PublishedProviderVersion> SaveVersion(PublishedProviderVersion newVersion, string partitionKey)
        {
            await _cosmosRepository.UpsertAsync(newVersion, partitionKey);

            return newVersion;
        }

        public Task<PublishedProviderVersion> CreateVersion(PublishedProviderVersion newVersion,
            PublishedProviderVersion currentVersion = null,
            string partitionKey = null,
            bool incrementFromCurrentVersion = false) =>
            throw new NotImplementedException();

        public Task<HttpStatusCode> SaveVersion(PublishedProviderVersion newVersion) => throw new NotImplementedException();
        public Task<HttpStatusCode> UpsertVersion(PublishedProviderVersion newVersion) => throw new NotImplementedException();
    }
}
