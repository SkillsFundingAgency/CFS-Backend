using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class PublishedProviderResultsRepository : IPublishedProviderResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public PublishedProviderResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
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
    }
}
