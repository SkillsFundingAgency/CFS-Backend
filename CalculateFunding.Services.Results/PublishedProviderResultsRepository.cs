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

        public Task CreatePublishedResults(IEnumerable<PublishedProviderResult> publishedResults)
        {
            Guard.ArgumentNotNull(publishedResults, nameof(publishedResults));

            return _cosmosRepository.BulkCreateAsync<PublishedProviderResult>(publishedResults.Select(m => new KeyValuePair<string, PublishedProviderResult>(m.ProviderId, m)));
        }

        public Task<IEnumerable<PublishedProviderResult>> GetPublishedProviderResultsForSpecificationId(string specificationId)
        {
            IQueryable<PublishedProviderResult> results = _cosmosRepository.Query<PublishedProviderResult>(enableCrossPartitionQuery: true).Where(m => m.SpecificationId == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }
    }
}
