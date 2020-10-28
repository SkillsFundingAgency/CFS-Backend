using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.Interfaces;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Results.Models;

namespace CalculateFunding.Services.Results.SearchIndex
{
    public class ProviderCalculationResultsIndexDataReader : ISearchIndexDataReader<ProviderResultDataKey, ProviderResult>
    {
        private readonly ICalculationResultsRepository _calculationResultsRepository;

        public ProviderCalculationResultsIndexDataReader(ICalculationResultsRepository calculationResultsRepository)
        {
            Guard.ArgumentNotNull(calculationResultsRepository, nameof(calculationResultsRepository));
            _calculationResultsRepository = calculationResultsRepository;
        }

        public async Task<ProviderResult> GetData(ProviderResultDataKey key)
        {
            return await _calculationResultsRepository.GetProviderResultById(key.ProviderResultId, key.PartitionKey);
        }
    }
}
