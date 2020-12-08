using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Services;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class PublishedFundingVersionBulkInMemoryRepository 
        : IVersionBulkRepository<PublishedFundingVersion>
    {
        private readonly ICosmosRepository _cosmosRepository;

        public PublishedFundingVersionBulkInMemoryRepository(
            ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public async Task<PublishedFundingVersion> SaveVersion(PublishedFundingVersion newVersion, string partitionKey)
        {
            await _cosmosRepository.UpsertAsync(newVersion, partitionKey);

            return newVersion;
        }
    }
}
