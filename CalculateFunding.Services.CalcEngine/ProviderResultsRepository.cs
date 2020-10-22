using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Results.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.CalcEngine.Caching;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using Newtonsoft.Json;
using Polly;
using Serilog;
using ProviderResult = CalculateFunding.Models.Calcs.ProviderResult;

namespace CalculateFunding.Services.CalcEngine
{
    public class ProviderResultsRepository : IProviderResultsRepository
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly ILogger _logger;
        private readonly ISearchRepository<ProviderCalculationResultsIndex> _providerCalculationResultsSearchRepository;
        private readonly IFeatureToggle _featureToggle;
        private readonly EngineSettings _engineSettings;
        private readonly IProviderResultCalculationsHashProvider _calculationsHashProvider;
        private readonly AsyncPolicy _resultsApiClientPolicy;
        private readonly IResultsApiClient _resultsApiClient;
        private readonly IJobManagement _jobManagement;

        public ProviderResultsRepository(
            ICosmosRepository cosmosRepository,
            ILogger logger,
            ISearchRepository<ProviderCalculationResultsIndex> providerCalculationResultsSearchRepository,
            IFeatureToggle featureToggle,
            EngineSettings engineSettings,
            IProviderResultCalculationsHashProvider calculationsHashProvider,
            ICalculatorResiliencePolicies calculatorResiliencePolicies,
            IResultsApiClient resultsApiClient,
            IJobManagement jobManagement)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(providerCalculationResultsSearchRepository, nameof(providerCalculationResultsSearchRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));
            Guard.ArgumentNotNull(calculationsHashProvider, nameof(calculationsHashProvider));
            Guard.ArgumentNotNull(calculatorResiliencePolicies, nameof(calculatorResiliencePolicies));
            Guard.ArgumentNotNull(resultsApiClient, nameof(resultsApiClient));
            Guard.ArgumentNotNull(calculatorResiliencePolicies.ResultsApiClient, nameof(calculatorResiliencePolicies.ResultsApiClient));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));

            _cosmosRepository = cosmosRepository;
            _logger = logger;
            _providerCalculationResultsSearchRepository = providerCalculationResultsSearchRepository;
            _featureToggle = featureToggle;
            _engineSettings = engineSettings;
            _calculationsHashProvider = calculationsHashProvider;
            _resultsApiClient = resultsApiClient;
            _resultsApiClientPolicy = calculatorResiliencePolicies.ResultsApiClient;
            _jobManagement = jobManagement;
        }

        public async Task<(long saveToCosmosElapsedMs, long saveToSearchElapsedMs, int savedProviders)> SaveProviderResults(
            IEnumerable<ProviderResult> providerResults,
            SpecificationSummary specificationSummary,
            int partitionIndex,
            int partitionSize,
            Reference user,
            string correlationId,
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


            IEnumerable<KeyValuePair<string, ProviderResult>> results = providerResults.Select(m => new KeyValuePair<string, ProviderResult>(m.Provider.Id, m));

            Task<long> cosmosSaveTask = BulkSaveProviderResults(results, degreeOfParallelism);
            Task<long> queueSearchWriterJobTask = QueueSearchIndexWriterJob(providerResults, specificationSummary, user, correlationId);

            await TaskHelper.WhenAllAndThrow(cosmosSaveTask, queueSearchWriterJobTask);

            // Only save batch to redis if it has been saved successfully. This enables the message to be requeued for throttled scenarios and will resave to cosmos/search
            _calculationsHashProvider.EndBatch(batchSpecificationId, partitionIndex, partitionSize);

            await QueueMergeSpecificationJobsInBatches(providerResults, specificationSummary);

            return (cosmosSaveTask.Result, queueSearchWriterJobTask.Result, providerResults.Count());
        }

        private async Task<long> QueueSearchIndexWriterJob(IEnumerable<ProviderResult> providerResults, SpecificationSummary specificationSummary, Reference user, string correlationId)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            string specificationId = specificationSummary.GetSpecificationId();
            IEnumerable<string> providerIds = providerResults.Select(x => x.Provider?.Id).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (!providerIds.Any())
            {
                return 0;
            }

            try
            {
                Job searchIndexWriterJob = await _jobManagement.QueueJob(
                    new JobCreateModel()
                    {
                        Trigger = new Trigger
                        {
                            EntityId = specificationId,
                            EntityType = "Specification",
                            Message = "Write ProviderCalculationResultsIndex serach index for specification"
                        },
                        InvokerUserId = user.Id,
                        InvokerUserDisplayName = user.Name,
                        JobDefinitionId = JobConstants.DefinitionNames.SearchIndexWriterJob,
                        ParentJobId = null,
                        SpecificationId = specificationId,
                        CorrelationId = correlationId,
                        Properties = new Dictionary<string, string>
                        {
                            {"specification-id", specificationId},
                            {"specification-name", specificationSummary.Name},
                            {"index-writer-type", SearchIndexWriterTypes.ProviderCalculationResultsIndexWriter }
                        },
                        MessageBody = JsonConvert.SerializeObject(providerIds)
                    });
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to queue SearchIndexWriterJob for specification - {specificationId}";
                _logger.Error(ex, errorMessage);
                throw;
            }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private bool ResultsHaveChanged(ProviderResult providerResult, int partitionIndex, int partitionSize)
        {
            bool hasChanged = _calculationsHashProvider.TryUpdateCalculationResultHash(providerResult, partitionIndex, partitionSize);

            if (!hasChanged)
            {
                _logger.Verbose(
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

        private async Task QueueMergeSpecificationJobsInBatches(IEnumerable<ProviderResult> providerResults,
            SpecificationSummary specification)
        {
            await _resultsApiClientPolicy.ExecuteAsync(() => _resultsApiClient.QueueMergeSpecificationInformationJob(new MergeSpecificationInformationRequest
            {
                SpecificationInformation = new SpecificationInformation
                {
                    Id = specification.Id,
                    Name = specification.Name,
                    FundingPeriodId = specification.FundingPeriod.Id,
                    FundingStreamIds = specification.FundingStreams?.Select(_ => _.Id).ToArray(),
                    LastEditDate = specification.LastEditedDate
                },
                ProviderIds = providerResults.Select(_ => _.Provider.Id).ToArray()
            }));
        }
    }
}
