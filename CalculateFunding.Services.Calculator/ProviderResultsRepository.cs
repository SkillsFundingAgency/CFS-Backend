using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calculator.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator
{
    public class ProviderResultsRepository : IProviderResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ProviderResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task SaveProviderResults(IEnumerable<ProviderResult> providerResults, int degreeOfParallelism = 5)
        {
            IEnumerable<KeyValuePair<string, ProviderResult>> results = providerResults.Select(m => new KeyValuePair<string, ProviderResult>(m.Provider.Id, m));
            return _cosmosRepository.BulkCreateAsync(results, degreeOfParallelism);
        }
    }
}
