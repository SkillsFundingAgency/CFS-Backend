using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using Serilog;

namespace CalculateFunding.Services.Calculator
{
    public class ProviderResultsRepository : IProviderResultsRepository
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly ISearchRepository<CalculationProviderResultsIndex> _searchRepository;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly ILogger _logger;
        private readonly ISearchRepository<ProviderCalculationResultsIndex> _providerCalculationResultsSearchRepository;
        private readonly IFeatureToggle _featureToggle;

        public ProviderResultsRepository(
            ICosmosRepository cosmosRepository,
            ISearchRepository<CalculationProviderResultsIndex> searchRepository,
            ISpecificationsRepository specificationsRepository,
            ILogger logger,
            ISearchRepository<ProviderCalculationResultsIndex> providerCalculationResultsSearchRepository,
            IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(providerCalculationResultsSearchRepository, nameof(providerCalculationResultsSearchRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _cosmosRepository = cosmosRepository;
            _searchRepository = searchRepository;
            _specificationsRepository = specificationsRepository;
            _logger = logger;
            _providerCalculationResultsSearchRepository = providerCalculationResultsSearchRepository;
            _featureToggle = featureToggle;
        }

        public async Task<(long saveToCosmosElapsedMs, long saveToSearchElapsedMs)> SaveProviderResults(IEnumerable<ProviderResult> providerResults, int degreeOfParallelism = 5)
        {
            if (providerResults == null || providerResults.Count() == 0)
            {
                return (0, 0);
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

            Task<long> cosmosSaveTask = BulkSaveProviderResults(results, degreeOfParallelism);
            Task<long> searchSaveTask = UpdateSearch(providerResults, specifications);

            await TaskHelper.WhenAllAndThrow(cosmosSaveTask, searchSaveTask);

            return (cosmosSaveTask.Result, searchSaveTask.Result);
        }

        private async Task<long> BulkSaveProviderResults(IEnumerable<KeyValuePair<string, ProviderResult>> providerResults, int degreeOfParallelism = 5)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            await _cosmosRepository.BulkUpsertAsync(providerResults, degreeOfParallelism);

            stopwatch.Stop();

            return stopwatch.ElapsedMilliseconds;
        }

        private async Task<long> UpdateSearch(IEnumerable<ProviderResult> providerResults, IDictionary<string, SpecificationSummary> specifications)
        {
            if (_featureToggle.IsNewProviderCalculationResultsIndexEnabled())
            {
                return await UpdateCalculationProviderResultsIndex(providerResults, specifications);
            }

            IList<CalculationProviderResultsIndex> results = new List<CalculationProviderResultsIndex>();

            foreach (ProviderResult providerResult in providerResults)
            {
                if (!providerResult.CalculationResults.IsNullOrEmpty())
                {
                    foreach (CalculationResult calculationResult in providerResult.CalculationResults.Where(m => m.CalculationSpecification != null))
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

            Stopwatch stopwatch = Stopwatch.StartNew();

            IEnumerable<IndexError> indexErrors = await _searchRepository.Index(results);

            stopwatch.Stop();

            if (!indexErrors.IsNullOrEmpty())
            {
                _logger.Error($"Failed to index provider results with the following errors: {string.Join(";", indexErrors.Select(m => m.ErrorMessage))}");

                // Throw exception so Service Bus message can be requeued and calc results can have a chance to get saved again
                throw new FailedToIndexSearchException(indexErrors);
            }

            return stopwatch.ElapsedMilliseconds;
        }

        private async Task<long> UpdateCalculationProviderResultsIndex(IEnumerable<ProviderResult> providerResults, IDictionary<string, SpecificationSummary> specifications)
        {
            Stopwatch assembleStopwatch = Stopwatch.StartNew();

            IList<ProviderCalculationResultsIndex> results = new List<ProviderCalculationResultsIndex>();

            foreach (ProviderResult providerResult in providerResults)
            {
                if (!providerResult.CalculationResults.IsNullOrEmpty())
                {
                    SpecificationSummary specification = specifications[providerResult.SpecificationId];

                    ProviderCalculationResultsIndex providerCalculationResultsIndex = new ProviderCalculationResultsIndex
                    {
                        SpecificationId = providerResult.SpecificationId,
                        SpecificationName = specification?.Name,
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
                        CalculationId = providerResult.CalculationResults.Select(m => m.Calculation.Id).ToArraySafe(),
                        CalculationName = providerResult.CalculationResults.Select(m => m.Calculation.Name).ToArraySafe(),
                        CalculationResult = providerResult.CalculationResults.Select(m => m.Value.HasValue ? m.Value.ToString() : "null").ToArraySafe()
					};

                    if (_featureToggle.IsExceptionMessagesEnabled())
                    {
                        providerCalculationResultsIndex.CalculationException = providerResult.CalculationResults
                            .Select(m => !string.IsNullOrWhiteSpace(m.ExceptionType) ? "true" : "false")
                            .ToArraySafe();

                        providerCalculationResultsIndex.CalculationExceptionType = providerResult.CalculationResults
                            .Select(m => m.ExceptionType ?? string.Empty)
                            .ToArraySafe();

                        providerCalculationResultsIndex.CalculationExceptionMessage = providerResult.CalculationResults
                            .Select(m => m.ExceptionMessage ?? string.Empty)
                            .ToArraySafe();
                    }

                    results.Add(providerCalculationResultsIndex);
                }
            }

            assembleStopwatch.Stop();

            Stopwatch stopwatch = Stopwatch.StartNew();

            IEnumerable<IndexError> indexErrors = await _providerCalculationResultsSearchRepository.Index(results);

            stopwatch.Stop();

            if (!indexErrors.IsNullOrEmpty())
            {
                _logger.Error($"Failed to index provider results with the following errors: {string.Join(";", indexErrors.Select(m => m.ErrorMessage))}");

                // Throw exception so Service Bus message can be requeued and calc results can have a chance to get saved again
                throw new FailedToIndexSearchException(indexErrors);
            }

            return stopwatch.ElapsedMilliseconds;
        }
    }
}
