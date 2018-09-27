using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Cosmos.Interfaces;
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
        private readonly ICosmosRepository _cosmosRepository;
        private readonly ISearchRepository<CalculationProviderResultsIndex> _searchRepository;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly ILogger _logger;

        public ProviderResultsRepository(
            ICosmosRepository cosmosRepository,
            ISearchRepository<CalculationProviderResultsIndex> searchRepository,
            ISpecificationsRepository specificationsRepository,
            ILogger logger)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _cosmosRepository = cosmosRepository;
            _searchRepository = searchRepository;
            _specificationsRepository = specificationsRepository;
            _logger = logger;
        }


        public async Task SaveProviderResults(IEnumerable<ProviderResult> providerResults, int degreeOfParallelism = 5)
        {
            if (providerResults == null || providerResults.Count() == 0)
            {
                return;
            }

            IEnumerable<KeyValuePair<string, ProviderResult>> results = providerResults.Select(m => new KeyValuePair<string, ProviderResult>(m.Provider.Id, m));

            IEnumerable<string> specificationIds = providerResults.Select(s => s.SpecificationId).Distinct();

            Dictionary<string, SpecificationSummary> specifications = new Dictionary<string, SpecificationSummary>();

            foreach (string specificationId in specificationIds)
            {
                SpecificationSummary specification = await _specificationsRepository.GetSpecificationSummaryById(specificationId);
                if (specification == null)
                {
                    throw new InvalidOperationException($"Result for Specification Summary lookup was null with ID '{specificationId}'");
                }

                specifications.Add(specificationId, specification);
            }

            await TaskHelper.WhenAllAndThrow(
                _cosmosRepository.BulkCreateAsync(results, degreeOfParallelism), UpdateSearch(providerResults, specifications));
        }

        private async Task UpdateSearch(IEnumerable<ProviderResult> providerResults, IDictionary<string, SpecificationSummary> specifications)
        {
            IList<CalculationProviderResultsIndex> results = new List<CalculationProviderResultsIndex>();

            foreach (ProviderResult providerResult in providerResults)
            {
                if (!providerResult.CalculationResults.IsNullOrEmpty())
                {
                    foreach (CalculationResult calculationResult in providerResult.CalculationResults)
                    {
                        SpecificationSummary specification = specifications[providerResult.SpecificationId];

                        results.Add(new CalculationProviderResultsIndex
                        {
                            SpecificationId = providerResult.SpecificationId,
                            SpecificationName = specification?.Name,
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
                            CalculationResult = calculationResult.Value.HasValue ? Convert.ToDouble(calculationResult.Value) : default(double?),
                            IsExcluded = !calculationResult.Value.HasValue
                        });
                    }
                }
            }

            IEnumerable<IndexError> indexErrors = await _searchRepository.Index(results);

            if (!indexErrors.IsNullOrEmpty())
            {
                _logger.Error($"Failed to index provider results with the following errors: {string.Join(";", indexErrors.Select(m => m.ErrorMessage))}");
            }
        }
    }
}
