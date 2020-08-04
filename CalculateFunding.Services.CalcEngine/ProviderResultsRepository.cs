using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Results.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CalcEngine.Caching;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using Polly;
using Serilog;
using ProviderResult = CalculateFunding.Models.Calcs.ProviderResult;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.CalcEngine
{
    public class ProviderResultsRepository : IProviderResultsRepository
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly ILogger _logger;
        private readonly ISearchRepository<ProviderCalculationResultsIndex> _providerCalculationResultsSearchRepository;
        private readonly IFeatureToggle _featureToggle;
        private readonly EngineSettings _engineSettings;
        private readonly IProviderResultCalculationsHashProvider _calculationsHashProvider;
        private readonly AsyncPolicy _specificationsApiClientPolicy;
        private readonly AsyncPolicy _resultsApiClientPolicy;
        private readonly IResultsApiClient _resultsApiClient;

        public ProviderResultsRepository(
            ICosmosRepository cosmosRepository,
            ISpecificationsApiClient specificationsApiClient,
            ILogger logger,
            ISearchRepository<ProviderCalculationResultsIndex> providerCalculationResultsSearchRepository,
            IFeatureToggle featureToggle,
            EngineSettings engineSettings,
            IProviderResultCalculationsHashProvider calculationsHashProvider,
            ICalculatorResiliencePolicies calculatorResiliencePolicies,
            IResultsApiClient resultsApiClient)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(providerCalculationResultsSearchRepository, nameof(providerCalculationResultsSearchRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));
            Guard.ArgumentNotNull(calculationsHashProvider, nameof(calculationsHashProvider));
            Guard.ArgumentNotNull(calculatorResiliencePolicies, nameof(calculatorResiliencePolicies));
            Guard.ArgumentNotNull(resultsApiClient, nameof(resultsApiClient));
            Guard.ArgumentNotNull(calculatorResiliencePolicies.SpecificationsApiClient, nameof(calculatorResiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(calculatorResiliencePolicies.ResultsApiClient, nameof(calculatorResiliencePolicies.ResultsApiClient));

            _cosmosRepository = cosmosRepository;
            _specificationsApiClient = specificationsApiClient;
            _logger = logger;
            _providerCalculationResultsSearchRepository = providerCalculationResultsSearchRepository;
            _featureToggle = featureToggle;
            _engineSettings = engineSettings;
            _calculationsHashProvider = calculationsHashProvider;
            _resultsApiClient = resultsApiClient;
            _specificationsApiClientPolicy = calculatorResiliencePolicies.SpecificationsApiClient;
            _resultsApiClientPolicy = calculatorResiliencePolicies.ResultsApiClient;
        }

        public async Task<(long saveToCosmosElapsedMs, long saveToSearchElapsedMs, int savedProviders)> SaveProviderResults(IEnumerable<ProviderResult> providerResults,
            int partitionIndex,
            int partitionSize,
            int degreeOfParallelism = 5)
        {
            if (providerResults == null || providerResults.Count() == 0)
            {
                return (0, 0, 0);
            }

            string batchSpecificationId = providerResults.First().SpecificationId;

            _calculationsHashProvider.StartBatch(batchSpecificationId, partitionIndex, partitionSize);

            //only leave the provider results where the calculation results have changed since they were last saved
            // ToArray required due to reevaulation of the providerResults further down
            providerResults = providerResults.Where(_ => ResultsHaveChanged(_, partitionIndex, partitionSize)).ToArray();

            _calculationsHashProvider.EndBatch(batchSpecificationId, partitionIndex, partitionSize);

            IEnumerable<KeyValuePair<string, ProviderResult>> results = providerResults.Select(m => new KeyValuePair<string, ProviderResult>(m.Provider.Id, m));

            IEnumerable<string> specificationIds = providerResults.Select(s => s.SpecificationId).Distinct();

            Dictionary<string, SpecModel.SpecificationSummary> specifications = new Dictionary<string, SpecModel.SpecificationSummary>();

            foreach (string specificationId in specificationIds)
            {
                ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse = 
                    await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

                if(!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
                {
                    throw new InvalidOperationException($"Result for Specification Summary lookup was null with ID '{specificationId}'");
                }

                SpecModel.SpecificationSummary specification = specificationApiResponse.Content;
                specifications.Add(specificationId, specification);
            }

            Task<long> cosmosSaveTask = BulkSaveProviderResults(results, degreeOfParallelism);
            Task<long> searchSaveTask = UpdateSearch(providerResults, specifications);

            await TaskHelper.WhenAllAndThrow(cosmosSaveTask, searchSaveTask);

            return (cosmosSaveTask.Result, searchSaveTask.Result, providerResults.Count());
        }

        private bool ResultsHaveChanged(ProviderResult providerResult, int partitionIndex, int partitionSize)
        {
            bool hasChanged = _calculationsHashProvider.TryUpdateCalculationResultHash(providerResult, partitionIndex, partitionSize);

            if (!hasChanged)
            {
                _logger.Information(
                    $"Provider:{providerResult.Provider.Id} Spec:{providerResult.SpecificationId} results have no changes so will not be stored this time");
            }

            return hasChanged;
        }

        private async Task<long> BulkSaveProviderResults(IEnumerable<KeyValuePair<string, ProviderResult>> providerResults, int degreeOfParallelism = 5)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            await _cosmosRepository.BulkUpsertAsync(providerResults, degreeOfParallelism: degreeOfParallelism, maintainCreatedDate: false);

            stopwatch.Stop();

            return stopwatch.ElapsedMilliseconds;
        }

        private async Task<long> UpdateSearch(IEnumerable<ProviderResult> providerResults, IDictionary<string, SpecModel.SpecificationSummary> specifications)
        {
            Stopwatch assembleStopwatch = Stopwatch.StartNew();

            List<ProviderCalculationResultsIndex> results = new List<ProviderCalculationResultsIndex>();

            foreach (ProviderResult providerResult in providerResults)
            {
                if (!providerResult.CalculationResults.IsNullOrEmpty())
                {
                    SpecModel.SpecificationSummary specification = specifications[providerResult.SpecificationId];

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
                        CalculationResult = providerResult.CalculationResults.Select(m => !string.IsNullOrEmpty(m.Value?.ToString()) ? m.Value.ToString() : "null").ToArraySafe(),
                    };

                    if(providerResult.FundingLineResults != null)
                    {
                        providerCalculationResultsIndex.FundingLineName = providerResult.FundingLineResults.Select(m => m.FundingLine.Name).ToArraySafe();
                        providerCalculationResultsIndex.FundingLineFundingStreamId = providerResult.FundingLineResults.Select(m => m.FundingLineFundingStreamId).ToArraySafe();
                        providerCalculationResultsIndex.FundingLineId = providerResult.FundingLineResults.Select(m => m.FundingLine.Id).ToArraySafe();
                        providerCalculationResultsIndex.FundingLineResult = providerResult.FundingLineResults.Select(m => !string.IsNullOrEmpty(m.Value?.ToString()) ? m.Value.ToString() : "null").ToArraySafe();
                    }

                    if (providerResult.Provider != null)
                    {
                        await _resultsApiClientPolicy.ExecuteAsync(() => _resultsApiClient.QueueMergeSpecificationInformationForProviderJobForProvider(new SpecificationInformation
                            {
                                Id = specification.Id,
                                Name = specification.Name,
                                FundingPeriodId = specification.FundingPeriod.Id,
                                LastEditDate = specification.LastEditedDate
                            },
                            providerResult.Provider.Id));
                    }

                    if (_featureToggle.IsExceptionMessagesEnabled())
                    {
                        providerCalculationResultsIndex.CalculationException = providerResult.CalculationResults
                            .Where(m => !string.IsNullOrWhiteSpace(m.ExceptionType))
                            .Select(e => e.Calculation.Id)
                            .ToArraySafe();

                        providerCalculationResultsIndex.CalculationExceptionType = providerResult.CalculationResults
                            .Select(m => m.ExceptionType ?? string.Empty)
                            .ToArraySafe();

                        providerCalculationResultsIndex.CalculationExceptionMessage = providerResult.CalculationResults
                            .Select(m => m.ExceptionMessage ?? string.Empty)
                            .ToArraySafe();

                        if (providerResult.FundingLineResults != null)
                        {
                            providerCalculationResultsIndex.FundingLineException = providerResult.FundingLineResults
                            .Where(m => !string.IsNullOrWhiteSpace(m.ExceptionType))
                            .Select(e => e.FundingLine.Id)
                            .ToArraySafe();

                            providerCalculationResultsIndex.FundingLineExceptionType = providerResult.FundingLineResults
                                .Select(m => m.ExceptionType ?? string.Empty)
                                .ToArraySafe();

                            providerCalculationResultsIndex.FundingLineExceptionMessage = providerResult.FundingLineResults
                                .Select(m => m.ExceptionMessage ?? string.Empty)
                                .ToArraySafe();
                        }
                    }

                    results.Add(providerCalculationResultsIndex);
                }
            }

            assembleStopwatch.Stop();

            Stopwatch stopwatch = Stopwatch.StartNew();

            foreach (IEnumerable<ProviderCalculationResultsIndex> resultsBatch in results.ToBatches(_engineSettings.CalculationResultSearchIndexBatchSize))
            {
                IEnumerable<IndexError> indexErrors = await _providerCalculationResultsSearchRepository.Index(resultsBatch);
                if (!indexErrors.IsNullOrEmpty())
                {
                    _logger.Error($"Failed to index provider results with the following errors: {string.Join(";", indexErrors.Select(m => m.ErrorMessage))}");

                    // Throw exception so Service Bus message can be requeued and calc results can have a chance to get saved again
                    throw new FailedToIndexSearchException(indexErrors);
                }
            }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
    }
}
