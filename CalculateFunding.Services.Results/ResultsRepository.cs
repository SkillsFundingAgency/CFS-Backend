using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results
{
    public class ResultsRepository : IResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

	    public Task<ProviderResult> GetProviderResult(string providerId, string specificationId)
	    {
		    var results = _cosmosRepository.Query<ProviderResult>().Where(x => x.Provider.Id == providerId && x.Specification.Id == specificationId).ToList().Take(1);

		    return Task.FromResult(results.FirstOrDefault());
		}

	    public Task<IEnumerable<ProviderResult>> GetSpecificationResults(string providerId)
	    {
		    var results = _cosmosRepository.Query<ProviderResult>().Where(x => x.Provider.Id == providerId);

		    return Task.FromResult(results.AsEnumerable());
		}

	    public async Task UpdateProviderResults(List<ProviderResult> results)
	    {
		    await _cosmosRepository.BulkCreateAsync(results, 50);
	    }
    }
}
