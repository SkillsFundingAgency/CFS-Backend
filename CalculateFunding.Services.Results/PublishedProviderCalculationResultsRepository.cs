using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class PublishedProviderCalculationResultsRepository : IPublishedProviderCalculationResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public PublishedProviderCalculationResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task CreatePublishedCalculationResults(IEnumerable<PublishedProviderCalculationResult> publishedCalculationResults)
        {
            Guard.ArgumentNotNull(publishedCalculationResults, nameof(publishedCalculationResults));

            return _cosmosRepository.BulkCreateAsync(publishedCalculationResults.Select(m => new KeyValuePair<string, PublishedProviderCalculationResult>(m.ProviderId, m)));
        }
    }
}
