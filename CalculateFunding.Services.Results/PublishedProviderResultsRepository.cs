using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Results.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class PublishedProviderResultsRepository : IPublishedProviderResultsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public PublishedProviderResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public Task SavePublishedResults(IEnumerable<PublishedProviderResult> publishedResults)
        {
            Guard.ArgumentNotNull(publishedResults, nameof(publishedResults));

            return _cosmosRepository.BulkCreateAsync<PublishedProviderResult>(publishedResults.Select(m => new KeyValuePair<string, PublishedProviderResult>(m.Provider.Id, m)));
        }

        public Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationId(string specificationId)
        {
            IQueryable<PublishedProviderResult> results = _cosmosRepository.Query<PublishedProviderResult>(enableCrossPartitionQuery: true).Where(m => m.SpecificationId == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }

        public Task<PublishedProviderResult> GetPublishedProviderResultForId(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            IQueryable<PublishedProviderResult> results = _cosmosRepository.Query<PublishedProviderResult>(enableCrossPartitionQuery: true).Where(m => m.Id == id && 
                m.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Published);

            return Task.FromResult(results.AsEnumerable().FirstOrDefault());
        }

        public Task<PublishedAllocationLineResultHistory> GetPublishedAllocationLineResultHistoryForId(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            IQueryable<PublishedAllocationLineResultHistory> results = _cosmosRepository.Query<PublishedAllocationLineResultHistory>(enableCrossPartitionQuery: true).Where(m => m.AllocationResultId == id);

            return Task.FromResult(results.AsEnumerable().FirstOrDefault());
        }

        public Task<IEnumerable<PublishedAllocationLineResultHistory>> GetPublishedProviderAllocationLineHistoryForSpecificationId(string specificationId)
        {
            IQueryable<PublishedAllocationLineResultHistory> results = _cosmosRepository.Query<PublishedAllocationLineResultHistory>(enableCrossPartitionQuery: true).Where(m => m.SpecificationId == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }

        public Task SavePublishedAllocationLineResultsHistory(IEnumerable<PublishedAllocationLineResultHistory> publishedResultsHistory)
        {
            Guard.ArgumentNotNull(publishedResultsHistory, nameof(publishedResultsHistory));

            return _cosmosRepository.BulkCreateAsync<PublishedAllocationLineResultHistory>(publishedResultsHistory.Select(m => new KeyValuePair<string, PublishedAllocationLineResultHistory>(m.ProviderId, m)));
        }

        public async Task<PublishedAllocationLineResultHistory> GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(string specificationId, string providerId, string allocationLineId)
        {
            string query = $"SELECT * from r where r.content.providerId = '{providerId}' and r.content.specificationId = '{specificationId}' and r.content.allocationLine.id = '{allocationLineId}' and r.documentType = 'PublishedAllocationLineResultHistory'";

            return ( await _cosmosRepository.QueryPartitionedEntity<PublishedAllocationLineResultHistory>(query, 1, providerId)).FirstOrDefault();
        }

        public async Task<IEnumerable<PublishedProviderResult>> GetAllNonHeldPublishedProviderResults()
        {
            IEnumerable<DocumentEntity<PublishedProviderResult>> documentEntities = await _cosmosRepository.GetAllDocumentsAsync<PublishedProviderResult>(
                query: m => m.Content.FundingStreamResult.AllocationLineResult.Current.Status != AllocationLineStatus.Held, enableCrossPartitionQuery: true);

            return documentEntities.Select(m => m.Content);
        }
    }
}
