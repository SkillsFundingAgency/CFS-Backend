using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calculator
{
    public class CalculationEngineService : ICalculationEngineService
    {
        private readonly ILogger _logger;
        private readonly ICalculationEngine _calculationEngine;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMessengerService _messengerService;
        private readonly IProviderSourceDatasetsRepository _providerSourceDatasetsRepository;
        private readonly ITelemetry _telemetry;
        private readonly IProviderResultsRepository _providerResultsRepository;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly EngineSettings _engineSettings;
        private readonly Policy _cacheProviderPolicy;
        private readonly Policy _messengerServicePolicy;
        private readonly Policy _providerSourceDatasetsRepositoryPolicy;
        private readonly Policy _providerResultsRepositoryPolicy;
        private readonly Policy _calculationsRepositoryPolicy;
        private readonly IValidator<ICalculatorResiliencePolicies> _calculatorResiliencePoliciesValidator;
        private readonly IDatasetAggregationsRepository _datasetAggregationsRepository;
        private readonly IFeatureToggle _featureToggle;
        private readonly IJobsRepository _jobsRepository;
        private readonly Policy _jobsRepositoryPolicy;

        public CalculationEngineService(
            ILogger logger,
            ICalculationEngine calculationEngine,
            ICacheProvider cacheProvider,
            IMessengerService messengerService,
            IProviderSourceDatasetsRepository providerSourceDatasetsRepository,
            ITelemetry telemetry,
            IProviderResultsRepository providerResultsRepository,
            ICalculationsRepository calculationsRepository,
            EngineSettings engineSettings,
            ICalculatorResiliencePolicies resiliencePolicies,
            IValidator<ICalculatorResiliencePolicies> calculatorResiliencePoliciesValidator,
            IDatasetAggregationsRepository datasetAggregationsRepository,
            IFeatureToggle featureToggle,
            IJobsRepository jobsRepository)
        {
            _calculatorResiliencePoliciesValidator = calculatorResiliencePoliciesValidator;

            CalculationEngineServiceValidator.ValidateConstruction(_calculatorResiliencePoliciesValidator,
                engineSettings, resiliencePolicies, calculationsRepository);

            _logger = logger;
            _calculationEngine = calculationEngine;
            _cacheProvider = cacheProvider;
            _messengerService = messengerService;
            _providerSourceDatasetsRepository = providerSourceDatasetsRepository;
            _telemetry = telemetry;
            _providerResultsRepository = providerResultsRepository;
            _calculationsRepository = calculationsRepository;
            _engineSettings = engineSettings;
            _cacheProviderPolicy = resiliencePolicies.CacheProvider;
            _messengerServicePolicy = resiliencePolicies.Messenger;
            _providerSourceDatasetsRepositoryPolicy = resiliencePolicies.ProviderSourceDatasetsRepository;
            _providerResultsRepositoryPolicy = resiliencePolicies.ProviderResultsRepository;
            _calculationsRepositoryPolicy = resiliencePolicies.CalculationsRepository;
            _datasetAggregationsRepository = datasetAggregationsRepository;
            _featureToggle = featureToggle;
            _jobsRepository = jobsRepository;
            _jobsRepositoryPolicy = resiliencePolicies.JobsRepository;
        }

        async public Task<IActionResult> GenerateAllocations(HttpRequest request)
        {
            string json = GetMessage();

            byte[] body = Encoding.ASCII.GetBytes(json);

            IDictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("sfa-correlationId", Guid.NewGuid().ToString());
            properties.Add("provider-summaries-partition-size", 1000);
            properties.Add("provider-summaries-partition-index", 5000);
            properties.Add("provider-cache-key", "add key here");
            properties.Add("specification-id", "add spec id here");

            Message message = new Message(body);
            message.PartitionKey = Guid.NewGuid().ToString();

            foreach (var property in properties)
            {
                message.UserProperties.Add(property.Key, property.Value);
            }

            await GenerateAllocations(message);

            return new NoContentResult();
        }

        public string GetMessage()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("Copy message here from dead letter");
            return sb.ToString();
        }

        public async Task GenerateAllocations(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            if (_featureToggle.IsJobServiceEnabled())
            {
                if (!message.UserProperties.ContainsKey("jobId"))
                {
                    _logger.Error("Missing job id for generating allocations");

                    return;
                }
                else
                {
                    string jobId = message.UserProperties["jobId"].ToString();

                    await _jobsRepositoryPolicy.ExecuteAsync(() => _jobsRepository.AddJobLog(jobId, new JobLogUpdateModel()));
                }
            }

            IEnumerable<ProviderSummary> summaries = null;

            string specificationId = message.UserProperties["specification-id"].ToString();

            _logger.Information($"Validating new allocations message");

            CalculationEngineServiceValidator.ValidateMessage(_logger, message);

            _logger.Information($"Generating allocations for specification id {specificationId}");

            BuildProject buildProject = await _calculationsRepository.GetBuildProjectBySpecificationId(specificationId);

            if (buildProject == null)
            {
                _logger.Error("A null build project was provided to UpdateAllocations");

                throw new ArgumentNullException(nameof(buildProject));
            }

            _logger.Information($"Fetched build project for id {specificationId}");

            int partitionIndex = int.Parse(message.UserProperties["provider-summaries-partition-index"].ToString());

            int partitionSize = int.Parse(message.UserProperties["provider-summaries-partition-size"].ToString());

            _logger.Information($"processing partition index {partitionIndex} for batch size {partitionSize}");

            int start = partitionIndex;

            int stop = start + partitionSize - 1;

            string cacheKey = message.UserProperties["provider-cache-key"].ToString();

            summaries = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.ListRangeAsync<ProviderSummary>(cacheKey, start, stop));

            int providerBatchSize = _engineSettings.ProviderBatchSize;

            Stopwatch calculationsLookupStopwatch = Stopwatch.StartNew();
            IEnumerable<CalculationSummaryModel> calculations = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationSummariesForSpecification(specificationId));
            if (calculations == null)
            {
                _logger.Error($"Calculations lookup API returned null for specification id {specificationId}");

                throw new InvalidOperationException("Calculations lookup API returned null");
            }
            calculationsLookupStopwatch.Stop();

            IEnumerable<DatasetAggregations> datasetAggregations = null;

            if (_featureToggle.IsAggregateSupportInCalculationsEnabled())
            {
                datasetAggregations = await _cacheProvider.GetAsync<List<DatasetAggregations>>($"{ CacheKeys.DatasetAggregationsForSpecification}{specificationId}");

                if (datasetAggregations.IsNullOrEmpty())
                {
                    datasetAggregations = await _datasetAggregationsRepository.GetDatasetAggregationsForSpecificationId(specificationId);

                    await _cacheProvider.SetAsync<List<DatasetAggregations>>($"{CacheKeys.DatasetAggregationsForSpecification}{specificationId}", datasetAggregations.ToList());
                }
            }

            int totalProviderResults = 0;

            for (int i = 0; i < summaries.Count(); i += providerBatchSize)
            {
                var calcTiming = Stopwatch.StartNew();

                ConcurrentBag<ProviderResult> providerResults = new ConcurrentBag<ProviderResult>();

                IEnumerable<ProviderSummary> partitionedSummaries = summaries.Skip(i).Take(providerBatchSize);

                IList<string> providerIdList = partitionedSummaries.Select(m => m.Id).ToList();

                Stopwatch providerSourceDatasetsStopwatch = Stopwatch.StartNew();

                _logger.Information($"Fetching provider sources for specification id {specificationId}");

                List<ProviderSourceDataset> providerSourceDatasets = new List<ProviderSourceDataset>(await _providerSourceDatasetsRepositoryPolicy.ExecuteAsync(() => _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(providerIdList, specificationId)));

                providerSourceDatasetsStopwatch.Stop();

                if (providerSourceDatasets == null)
                {
                    _logger.Information($"No provider sources found for specification id {specificationId}");

                    providerSourceDatasets = new List<ProviderSourceDataset>();
                }

                _logger.Information($"fetched provider sources found for specification id {specificationId}");

                Stopwatch calculationStopwatch = Stopwatch.StartNew();

                _logger.Information($"calculating results for specification id {specificationId}");

                Assembly assembly = Assembly.Load(Convert.FromBase64String(buildProject.Build.AssemblyBase64));
                Parallel.ForEach(partitionedSummaries, new ParallelOptions { MaxDegreeOfParallelism = _engineSettings.CalculateProviderResultsDegreeOfParallelism }, provider =>
                {
                    IAllocationModel allocationModel = _calculationEngine.GenerateAllocationModel(assembly);

                    IEnumerable<ProviderSourceDataset> providerDatasets = providerSourceDatasets.Where(m => m.ProviderId == provider.Id);

                    ProviderResult result = _calculationEngine.CalculateProviderResults(allocationModel, buildProject, calculations, provider, providerDatasets, datasetAggregations);

                    if (result != null)
                    {
                        providerResults.Add(result);
                    }
                    else
                    {
                        throw new InvalidOperationException("Null result from Calc Engine CalculateProviderResults");
                    }
                });

                _logger.Information($"calculating results complete for specification id {specificationId}");

                calculationStopwatch.Stop();

                double? saveCosmosElapsedMs = null;
                double saveRedisElapsedMs = 0;
                double saveQueueElapsedMs = 0;

                if (providerResults.Any())
                {
                    if (!message.UserProperties.ContainsKey("ignore-save-provider-results"))
                    {
                        _logger.Information($"Saving results for specification id {specificationId}");

                        Stopwatch saveCosmosStopwatch = Stopwatch.StartNew();
                        await _providerResultsRepositoryPolicy.ExecuteAsync(() => _providerResultsRepository.SaveProviderResults(providerResults, _engineSettings.SaveProviderDegreeOfParallelism));
                        saveCosmosStopwatch.Stop();
                        saveCosmosElapsedMs = saveCosmosStopwatch.ElapsedMilliseconds;

                        _logger.Information($"Saving results completeed for specification id {specificationId}");
                    }

                    // Should just be the GUID as the content, as the prefix is read by the receiver, rather than the sender
                    string providerResultsCacheKey = Guid.NewGuid().ToString();

                    _logger.Information($"Saving results to cache for specification id {specificationId} with key {providerResultsCacheKey}");

                    Stopwatch saveRedisStopwatch = Stopwatch.StartNew();
                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync<List<ProviderResult>>($"{CacheKeys.ProviderResultBatch}{providerResultsCacheKey}", providerResults.ToList(), TimeSpan.FromHours(12), false));
                    saveRedisStopwatch.Stop();

                    _logger.Information($"Saved results to cache for specification id {specificationId} with key {providerResultsCacheKey}");

                    saveRedisElapsedMs = saveRedisStopwatch.ElapsedMilliseconds;

                    IDictionary<string, string> properties = message.BuildMessageProperties();

                    properties.Add("specificationId", specificationId);

                    properties.Add("providerResultsCacheKey", providerResultsCacheKey);

                    _logger.Information($"Sending message for test exceution for specification id {specificationId}");

                    Stopwatch saveQueueStopwatch = Stopwatch.StartNew();
                    await _messengerServicePolicy.ExecuteAsync(() => _messengerService.SendToQueue<string>(ServiceBusConstants.QueueNames.TestEngineExecuteTests, null, properties));
                    saveQueueStopwatch.Stop();

                    saveQueueElapsedMs = saveQueueStopwatch.ElapsedMilliseconds;

                    _logger.Information($"Message sent for test exceution for specification id {specificationId}");

                    totalProviderResults += providerResults.Count();
                }

                calcTiming.Stop();

                IDictionary<string, double> metrics = new Dictionary<string, double>()
                    {
                        { "calculation-run-providersProcessed", partitionedSummaries.Count() },
                        { "calculation-run-lookupCalculationDefinitionsMs", calculationsLookupStopwatch.ElapsedMilliseconds },
                        { "calculation-run-providersResultsFromCache", summaries.Count() },
                        { "calculation-run-partitionSize", partitionSize },
                        { "calculation-run-providerSourceDatasetQueryMs", providerSourceDatasetsStopwatch.ElapsedMilliseconds },
                        { "calculation-run-saveProviderResultsRedisMs", saveRedisElapsedMs },
                        { "calculation-run-saveProviderResultsServiceBusMs", saveQueueElapsedMs },
                        { "calculation-run-runningCalculationMs", calculationStopwatch.ElapsedMilliseconds },
                    };

                if (saveCosmosElapsedMs.HasValue)
                {
                    metrics.Add("calculation-run-elapsedMilliseconds", calcTiming.ElapsedMilliseconds);
                    metrics.Add("calculation-run-saveProviderResultsCosmosMs", saveCosmosElapsedMs.Value);
                }
                else
                {
                    metrics.Add("calculation-run-for-tests-ms", calcTiming.ElapsedMilliseconds);
                }

                _telemetry.TrackEvent("CalculationRunProvidersProcessed",
                    new Dictionary<string, string>()
                    {
                        { "specificationId" , specificationId },
                        { "buildProjectId" , buildProject.Id },
                    },
                    metrics
                );
            }

            if (_featureToggle.IsJobServiceEnabled())
            {
                string jobId = message.UserProperties["jobId"].ToString();

                int itemsProcessed = summaries.Count();
                int itemsSucceeded = totalProviderResults;
                int itemsFailed = itemsProcessed - itemsSucceeded;

                await _jobsRepositoryPolicy.ExecuteAsync(() => _jobsRepository.AddJobLog(jobId, new JobLogUpdateModel
                {
                    CompletedSuccessfully = true,
                    ItemsSucceeded = itemsSucceeded,
                    ItemsFailed = itemsFailed,
                    ItemsProcessed = itemsProcessed,
                    Outcome = $"{itemsSucceeded} provider results were generated successfully from {itemsProcessed} providers"
                }));
            }
        }

        public async Task UpdateDeadLetteredJobLog(Message message)
        {
            if (!_featureToggle.IsJobServiceEnabled())
            {
                return;
            }

            Guard.ArgumentNotNull(message, nameof(message));

            if (!message.UserProperties.ContainsKey("jobId"))
            {
                _logger.Error("Missing job id from dead lettered message");
                return;
            }

            string jobId = message.UserProperties["jobId"].ToString();

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = $"The job has exceeded its maximum retry count and failed to complete successfully"
            };

            try
            {
                JobLog jobLog = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobsRepository.AddJobLog(jobId, jobLogUpdateModel));

                _logger.Information($"A new job log was added to inform of a dead lettered message with job log id '{jobLog.Id}' on job with id '{jobId}' while attempting to generate allocations");
            }
            catch(Exception exception)
            {
                _logger.Error(exception, $"Failed to add a job log for job id '{jobId}'");
            }
        }
    }
}
