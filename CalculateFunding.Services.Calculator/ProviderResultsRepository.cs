using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator
{
    public class ProviderResultsRepository : IProviderResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;
        private readonly ISearchRepository<CalculationProviderResultsIndex> _searchRepository;
        private readonly ILogger _logger;

        public ProviderResultsRepository(CosmosRepository cosmosRepository, 
            ISearchRepository<CalculationProviderResultsIndex> searchRepository, ILogger logger)
        {
            _cosmosRepository = cosmosRepository;
            _searchRepository = searchRepository;
            _logger = logger;
        }

        public async Task SaveProviderResults(IEnumerable<ProviderResult> providerResults, int degreeOfParallelism = 5)
        {
            IEnumerable<KeyValuePair<string, ProviderResult>> results = providerResults.Select(m => new KeyValuePair<string, ProviderResult>(m.Provider.Id, m));

            await TaskHelper.WhenAllAndThrow(
                _cosmosRepository.BulkCreateAsync(results, degreeOfParallelism), UpdateSearch(providerResults));
        }

        async Task UpdateSearch(IEnumerable<ProviderResult> providerResults)
        {
            IList<CalculationProviderResultsIndex> results = new List<CalculationProviderResultsIndex>();

            foreach(ProviderResult providerResult in providerResults)
            {
                if (!providerResult.CalculationResults.IsNullOrEmpty())
                {
                    foreach (CalculationResult calculationResult in providerResult.CalculationResults)
                    {
                        if (calculationResult.Value.HasValue)
                        {
                            results.Add(new CalculationProviderResultsIndex
                            {
                                SpecificationId = providerResult.Specification?.Id,
                                SpecificationName = providerResult.Specification?.Name,
                                CalculationSpecificationId = calculationResult.CalculationSpecification?.Id,
                                CalculationSpecificationName = calculationResult.CalculationSpecification?.Name,
                                CalculationName = calculationResult.Calculation?.Name,
                                CalculationId = calculationResult.Calculation?.Id,
                                CalculationType = calculationResult.CalculationType.ToString(),
                                ProviderId = providerResult.Provider?.Id,
                                ProviderName = providerResult.Provider?.Name,
                                ProviderType = providerResult.Provider?.ProviderType,
                                ProviderSubType = providerResult.Provider?.ProviderSubType,
                                LocalAuthority = providerResult.Provider?.Authority,
                                LastUpdatedDate = DateTimeOffset.Now,
                                UKPRN = providerResult.Provider?.UKPRN,
                                URN = providerResult.Provider?.URN,
                                UPIN = providerResult.Provider?.UPIN,
                                EstablishmentNumber = providerResult.Provider?.EstablishmentNumber,
                                OpenDate = providerResult.Provider?.DateOpened,
                                CaclulationResult = calculationResult.Value.HasValue ? Convert.ToDouble(calculationResult.Value) : 0
                            });
                        }
                    }
                }
            }

            IEnumerable<IndexError> indexErrors = await _searchRepository.Index(results);

            if (indexErrors.Any())
            {
                _logger.Error($"Failed to index provider results with the following errors: {string.Join(";", indexErrors.Select(m => m.ErrorMessage))}");
            }
        }
    }
}
