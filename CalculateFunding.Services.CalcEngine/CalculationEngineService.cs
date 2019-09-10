using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.CalcEngine
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
        private readonly IJobsApiClient _jobsApiClient;
        private readonly Policy _jobsApiClientPolicy;

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
            IJobsApiClient jobsApiClient)
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
            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = resiliencePolicies.JobsApiClient;
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

            foreach (KeyValuePair<string, object> property in properties)
            {
                message.UserProperties.Add(property.Key, property.Value);
            }

            await GenerateAllocations(message);

            return new NoContentResult();
        }

        public string GetMessage()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Copy message here from dead letter");
            return sb.ToString();
        }

        public async Task GenerateAllocations(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            _logger.Information($"Validating new allocations message");

            CalculationEngineServiceValidator.ValidateMessage(_logger, message);

            GenerateAllocationMessageProperties messageProperties = GetMessageProperties(message);

            JobViewModel job = await AddStartingProcessJobLog(messageProperties.JobId);

            if (job == null)
            {
                return;
            }

            messageProperties.GenerateCalculationAggregationsOnly = (job.JobDefinitionId == JobConstants.DefinitionNames.GenerateCalculationAggregationsJob);

            IEnumerable<ProviderSummary> summaries = null;

            _logger.Information($"Generating allocations for specification id {messageProperties.SpecificationId}");

            BuildProject buildProject = await GetBuildProject(messageProperties.SpecificationId);

            byte[] assembly = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetAssemblyBySpecificationId(messageProperties.SpecificationId));

            if (assembly == null)
            {
                string error = $"Failed to get assembly for specification Id '{messageProperties.SpecificationId}'";
                _logger.Error(error);
                throw new RetriableException(error);
            }

            buildProject.Build.Assembly = assembly;

            Dictionary<string, List<decimal>> cachedCalculationAggregationsBatch = CreateCalculationAggregateBatchDictionary(messageProperties);

            _logger.Information($"processing partition index {messageProperties.PartitionIndex} for batch size {messageProperties.PartitionSize}");

            int start = messageProperties.PartitionIndex;

            int stop = start + messageProperties.PartitionSize - 1;

            summaries = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.ListRangeAsync<ProviderSummary>(messageProperties.ProviderCacheKey, start, stop));

            int providerBatchSize = _engineSettings.ProviderBatchSize;

            Stopwatch calculationsLookupStopwatch = Stopwatch.StartNew();
            IEnumerable<CalculationSummaryModel> calculations = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationSummariesForSpecification(messageProperties.SpecificationId));
            if (calculations == null)
            {
                _logger.Error($"Calculations lookup API returned null for specification id {messageProperties.SpecificationId}");

                throw new InvalidOperationException("Calculations lookup API returned null");
            }
            calculationsLookupStopwatch.Stop();

            IEnumerable<CalculationAggregation> aggregations = await BuildAggregations(messageProperties);

            int totalProviderResults = 0;

            bool calculationResultsHaveExceptions = false;

            for (int i = 0; i < summaries.Count(); i += providerBatchSize)
            {
                Stopwatch calculationStopwatch = new Stopwatch();
                Stopwatch providerSourceDatasetsStopwatch = new Stopwatch();

                Stopwatch calcTiming = Stopwatch.StartNew();

                CalculationResultsModel calculationResults = await CalculateResults(summaries, calculations, aggregations, buildProject, messageProperties, providerBatchSize, i, providerSourceDatasetsStopwatch, calculationStopwatch);

                _logger.Information($"calculating results complete for specification id {messageProperties.SpecificationId}");

                long saveCosmosElapsedMs = -1;
                long saveSearchElapsedMs = -1;
                long saveRedisElapsedMs = 0;
                long saveQueueElapsedMs = 0;
                int savedProviders = 0;
                int percentageProvidersSaved = 0;

                if (calculationResults.ProviderResults.Any())
                {
                    if (messageProperties.GenerateCalculationAggregationsOnly)
                    {
                        PopulateCachedCalculationAggregationsBatch(calculationResults.ProviderResults, cachedCalculationAggregationsBatch, messageProperties);
                    }
                    else
                    {
                        (long saveCosmosElapsedMs, long saveSearchElapsedMs, long saveRedisElapsedMs, long saveQueueElapsedMs, int savedProviders) processResultsMetrics = 
                            await ProcessProviderResults(calculationResults.ProviderResults, messageProperties, message);
                        
                        saveCosmosElapsedMs = processResultsMetrics.saveCosmosElapsedMs;
                        saveSearchElapsedMs = processResultsMetrics.saveSearchElapsedMs;
                        saveRedisElapsedMs = processResultsMetrics.saveRedisElapsedMs;
                        saveQueueElapsedMs = processResultsMetrics.saveQueueElapsedMs;
                        savedProviders = processResultsMetrics.savedProviders;
                        
                        totalProviderResults += calculationResults.ProviderResults.Count();
                        percentageProvidersSaved = savedProviders / totalProviderResults * 100;

                        if (calculationResults.ResultsContainExceptions)
                        {
                            if (!calculationResultsHaveExceptions)
                            {
                                calculationResultsHaveExceptions = true;
                            }
                        }
                    }

                }

                calcTiming.Stop();

                IDictionary<string, double> metrics = new Dictionary<string, double>()
                {
                    { "calculation-run-providersProcessed", calculationResults.PartitionedSummaries.Count() },
                    { "calculation-run-lookupCalculationDefinitionsMs", calculationsLookupStopwatch.ElapsedMilliseconds },
                    { "calculation-run-providersResultsFromCache", summaries.Count() },
                    { "calculation-run-partitionSize", messageProperties.PartitionSize },
                    { "calculation-run-providerSourceDatasetQueryMs", providerSourceDatasetsStopwatch.ElapsedMilliseconds },
                    { "calculation-run-saveProviderResultsRedisMs", saveRedisElapsedMs },
                    { "calculation-run-saveProviderResultsServiceBusMs", saveQueueElapsedMs },
                    { "calculation-run-runningCalculationMs",  calculationStopwatch.ElapsedMilliseconds },
                    { "calculation-run-savedProviders",  savedProviders },
                    { "calculation-run-savePercentage ",  percentageProvidersSaved },
                };

                if (saveCosmosElapsedMs > -1)
                {
                    metrics.Add("calculation-run-elapsedMilliseconds", calcTiming.ElapsedMilliseconds);
                    metrics.Add("calculation-run-saveProviderResultsCosmosMs", saveCosmosElapsedMs);
                    metrics.Add("calculation-run-saveProviderResultsSearchMs", saveSearchElapsedMs);
                }
                else
                {
                    metrics.Add("calculation-run-for-tests-ms", calcTiming.ElapsedMilliseconds);
                }


                _telemetry.TrackEvent("CalculationRunProvidersProcessed",
                    new Dictionary<string, string>()
                    {
                    { "specificationId" , messageProperties.SpecificationId },
                    { "buildProjectId" , buildProject.Id },
                    },
                    metrics
                );
            }

            if (calculationResultsHaveExceptions)
            {
                await FailJob(messageProperties.JobId, totalProviderResults, "Exceptions were thrown during generation of calculation results");
            }
            else
            {
                await CompleteBatch(messageProperties, cachedCalculationAggregationsBatch, summaries.Count(), totalProviderResults);
            }
        }

        private async Task<CalculationResultsModel> CalculateResults(IEnumerable<ProviderSummary> summaries, IEnumerable<CalculationSummaryModel> calculations, IEnumerable<CalculationAggregation> aggregations, BuildProject buildProject,
            GenerateAllocationMessageProperties messageProperties, int providerBatchSize, int index, Stopwatch providerSourceDatasetsStopwatch, Stopwatch calculationStopwatch)
        {
            ConcurrentBag<ProviderResult> providerResults = new ConcurrentBag<ProviderResult>();

            IEnumerable<ProviderSummary> partitionedSummaries = summaries.Skip(index).Take(providerBatchSize);

            IList<string> providerIdList = partitionedSummaries.Select(m => m.Id).ToList();

            providerSourceDatasetsStopwatch.Start();

            _logger.Information($"Fetching provider sources for specification id {messageProperties.SpecificationId}");

            List<ProviderSourceDataset> providerSourceDatasets = new List<ProviderSourceDataset>(await _providerSourceDatasetsRepositoryPolicy.ExecuteAsync(() => _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(providerIdList, messageProperties.SpecificationId)));

            providerSourceDatasetsStopwatch.Stop();

            _logger.Information($"fetched provider sources found for specification id {messageProperties.SpecificationId}");

            calculationStopwatch.Start();

            _logger.Information($"calculating results for specification id {messageProperties.SpecificationId}");

            Assembly assembly = Assembly.Load(buildProject.Build.Assembly);

            Parallel.ForEach(partitionedSummaries, new ParallelOptions { MaxDegreeOfParallelism = _engineSettings.CalculateProviderResultsDegreeOfParallelism }, provider =>
            {
                IAllocationModel allocationModel = _calculationEngine.GenerateAllocationModel(assembly);

                IEnumerable<ProviderSourceDataset> providerDatasets = providerSourceDatasets.Where(m => m.ProviderId == provider.Id);

                ProviderResult result = _calculationEngine.CalculateProviderResults(allocationModel, buildProject, calculations, provider, providerDatasets, aggregations);

                if (result == null)
                {
                    throw new InvalidOperationException("Null result from Calc Engine CalculateProviderResults");
                }
                
                providerResults.Add(result);
            });


            _logger.Information($"calculating results complete for specification id {messageProperties.SpecificationId}");

            calculationStopwatch.Stop();

            return new CalculationResultsModel
            {
                ProviderResults = providerResults,
                PartitionedSummaries = partitionedSummaries
            };
        }

        private GenerateAllocationMessageProperties GetMessageProperties(Message message)
        {
            GenerateAllocationMessageProperties properties = new GenerateAllocationMessageProperties();

            if (message.UserProperties.ContainsKey("jobId"))
            {
                properties.JobId = message.UserProperties["jobId"].ToString();
            }
            else
            {
                _logger.Error("Missing job id for generating allocations");
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            properties.SpecificationId = specificationId;

            int batchNumber = 0;

            if (message.UserProperties.ContainsKey("batch-number"))
            {
                batchNumber = int.Parse(message.UserProperties["batch-number"].ToString());
            }

            int batchCount = 0;

            if (message.UserProperties.ContainsKey("batch-count"))
            {
                batchCount = int.Parse(message.UserProperties["batch-count"].ToString());
            }

            properties.BatchNumber = batchNumber;

            properties.BatchCount = batchCount;

            properties.ProviderCacheKey = message.UserProperties["provider-cache-key"].ToString();

            properties.PartitionIndex = int.Parse(message.UserProperties["provider-summaries-partition-index"].ToString());

            properties.PartitionSize = int.Parse(message.UserProperties["provider-summaries-partition-size"].ToString());

            properties.CalculationsAggregationsBatchCacheKey = $"{CacheKeys.CalculationAggregations}{specificationId}_{batchNumber}";

            properties.CalculationsToAggregate = message.UserProperties.ContainsKey("calculations-to-aggregate") && !string.IsNullOrWhiteSpace(message.UserProperties["calculations-to-aggregate"].ToString()) ? message.UserProperties["calculations-to-aggregate"].ToString().Split(',') : null;

            return properties;
        }

        private async Task<JobViewModel> AddStartingProcessJobLog(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                _logger.Error($"No jobId given.");
                throw new NonRetriableException("No Job Id given");
            }

            ApiResponse<JobViewModel> jobResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.GetJobById(jobId));

            if (jobResponse == null || jobResponse.Content == null)
            {
                _logger.Error($"Could not find the parent job with job id: '{jobId}'");

                throw new NonRetriableException($"Could not find the parent job with job id: '{jobId}'");
            }

            JobViewModel job = jobResponse.Content;

            if (job.CompletionStatus.HasValue)
            {
                _logger.Information($"Received job with id: '{job.Id}' is already in a completed state with status {job.CompletionStatus.ToString()}");

                return null;
            }

            await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, new JobLogUpdateModel()));

            return job;
        }

        private async Task<BuildProject> GetBuildProject(string specificationId)
        {
            BuildProject buildProject = await _calculationsRepository.GetBuildProjectBySpecificationId(specificationId);

            if (buildProject == null)
            {
                _logger.Error("A null build project was provided to UpdateAllocations");

                throw new ArgumentNullException(nameof(buildProject));
            }

            _logger.Information($"Fetched build project for id {specificationId}");

            return buildProject;
        }

        private Dictionary<string, List<decimal>> CreateCalculationAggregateBatchDictionary(GenerateAllocationMessageProperties messageProperties)
        {
            if (!messageProperties.GenerateCalculationAggregationsOnly)
            {
                return null;
            }

            Dictionary<string, List<decimal>> cachedCalculationAggregationsBatch = new Dictionary<string, List<decimal>>(StringComparer.InvariantCultureIgnoreCase);

            if (!messageProperties.CalculationsToAggregate.IsNullOrEmpty())
            {
                foreach (string calcToAggregate in messageProperties.CalculationsToAggregate)
                {
                    if (!cachedCalculationAggregationsBatch.ContainsKey(calcToAggregate))
                    {
                        cachedCalculationAggregationsBatch.Add(calcToAggregate, new List<decimal>());
                    }
                }
            }

            return cachedCalculationAggregationsBatch;
        }

        private async Task<IEnumerable<CalculationAggregation>> BuildAggregations(GenerateAllocationMessageProperties messageProperties)
        {
            IEnumerable<CalculationAggregation> aggregations = Enumerable.Empty<CalculationAggregation>();


            aggregations = await _cacheProvider.GetAsync<List<CalculationAggregation>>($"{ CacheKeys.DatasetAggregationsForSpecification}{messageProperties.SpecificationId}");

            if (aggregations.IsNullOrEmpty())
            {
                aggregations = (await _datasetAggregationsRepository.GetDatasetAggregationsForSpecificationId(messageProperties.SpecificationId)).Select(m => new CalculationAggregation
                {
                    SpecificationId = m.SpecificationId,
                    Values = m.Fields.IsNullOrEmpty() ? Enumerable.Empty<AggregateValue>() : m.Fields.Select(f => new AggregateValue
                    {
                        AggregatedType = f.FieldType,
                        FieldDefinitionName = f.FieldDefinitionName,
                        Value = f.Value
                    })
                });

                await _cacheProvider.SetAsync<List<CalculationAggregation>>($"{CacheKeys.DatasetAggregationsForSpecification}{messageProperties.SpecificationId}", aggregations.ToList());
            }

            if (!messageProperties.GenerateCalculationAggregationsOnly)
            {
                Dictionary<string, List<decimal>> cachedCalculationAggregations = new Dictionary<string, List<decimal>>(StringComparer.InvariantCultureIgnoreCase);

                for (int i = 1; i <= messageProperties.BatchCount; i++)
                {
                    string batchedCacheKey = $"{CacheKeys.CalculationAggregations}{messageProperties.SpecificationId}_{i}";

                    Dictionary<string, List<decimal>> cachedCalculationAggregationsPart = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<Dictionary<string, List<decimal>>>(batchedCacheKey));

                    if (!cachedCalculationAggregationsPart.IsNullOrEmpty())
                    {
                        foreach (KeyValuePair<string, List<decimal>> cachedAggregations in cachedCalculationAggregationsPart)
                        {
                            if (!cachedCalculationAggregations.ContainsKey(cachedAggregations.Key))
                            {
                                cachedCalculationAggregations.Add(cachedAggregations.Key, new List<decimal>());
                            }

                            cachedCalculationAggregations[cachedAggregations.Key].AddRange(cachedAggregations.Value);
                        }
                    }
                }

                if (!cachedCalculationAggregations.IsNullOrEmpty())
                {
                    foreach (KeyValuePair<string, List<decimal>> cachedCalculationAggregation in cachedCalculationAggregations)
                    {
                        aggregations = aggregations.Concat(new[]
                        {
                                new CalculationAggregation
                                {
                                    SpecificationId = messageProperties.SpecificationId,
                                    Values = new []
                                    {
                                        new AggregateValue { FieldDefinitionName = cachedCalculationAggregation.Key, AggregatedType = AggregatedType.Sum, Value = cachedCalculationAggregation.Value.Sum()},
                                        new AggregateValue { FieldDefinitionName = cachedCalculationAggregation.Key, AggregatedType = AggregatedType.Min, Value = cachedCalculationAggregation.Value.Min()},
                                        new AggregateValue { FieldDefinitionName = cachedCalculationAggregation.Key, AggregatedType = AggregatedType.Max, Value = cachedCalculationAggregation.Value.Max()},
                                        new AggregateValue { FieldDefinitionName = cachedCalculationAggregation.Key, AggregatedType = AggregatedType.Average, Value = cachedCalculationAggregation.Value.Average()},
                                    }
                                }
                            });
                    }
                }
            }

            return aggregations;
        }

        private async Task CompleteBatch(GenerateAllocationMessageProperties messageProperties, Dictionary<string, List<decimal>> cachedCalculationAggregationsBatch, int itemsProcessed, int totalProviderResults)
        {
            int itemsSucceeded = totalProviderResults;
            int itemsFailed = itemsProcessed - itemsSucceeded;
            string outcome = $"{itemsSucceeded} provider results were generated successfully from {itemsProcessed} providers";

            if (messageProperties.GenerateCalculationAggregationsOnly)
            {
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync<Dictionary<string, List<decimal>>>(messageProperties.CalculationsAggregationsBatchCacheKey, cachedCalculationAggregationsBatch));

                outcome = $"{itemsSucceeded} provider result calculation aggregations were generated successfully from {itemsProcessed} providers";
            }

            await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(messageProperties.JobId, new JobLogUpdateModel
            {
                CompletedSuccessfully = true,
                ItemsSucceeded = itemsSucceeded,
                ItemsFailed = itemsFailed,
                ItemsProcessed = itemsProcessed,
                Outcome = outcome
            }));
        }

        private void PopulateCachedCalculationAggregationsBatch(IEnumerable<ProviderResult> providerResults, Dictionary<string, List<decimal>> cachedCalculationAggregationsBatch, GenerateAllocationMessageProperties messageProperties)
        {
            if (cachedCalculationAggregationsBatch == null)
            {
                _logger.Error($"Cached calculation aggregations not found for key: {messageProperties.CalculationsAggregationsBatchCacheKey}");

                throw new Exception($"Cached calculation aggregations not found for key: {messageProperties.CalculationsAggregationsBatchCacheKey}");
            }

            IEnumerable<string> calculationsToAggregate = messageProperties.CalculationsToAggregate;

            foreach (ProviderResult providerResult in providerResults)
            {
                IEnumerable<CalculationResult> calculationResultsForAggregation = providerResult.CalculationResults.Where(m => 
                    calculationsToAggregate.Contains(VisualBasicTypeGenerator.GenerateIdentifier(m.Calculation.Name), StringComparer.InvariantCultureIgnoreCase));

                foreach (CalculationResult calculationResult in calculationResultsForAggregation)
                {
                    string calculationReferenceName = CalculationTypeGenerator.GenerateIdentifier(calculationResult.Calculation.Name.Trim());

                    string calcNameFromCalcsToAggregate = messageProperties.CalculationsToAggregate.FirstOrDefault(m => string.Equals(m, calculationReferenceName, StringComparison.InvariantCultureIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(calcNameFromCalcsToAggregate) && cachedCalculationAggregationsBatch.ContainsKey(calculationReferenceName))
                    {
                        cachedCalculationAggregationsBatch[calcNameFromCalcsToAggregate].Add(calculationResult.Value.HasValue ? calculationResult.Value.Value : 0);
                    }

                }
            }
        }

        private async Task<(long saveCosmosElapsedMs, long saveToSearchElapsedMs, long saveRedisElapsedMs, long saveQueueElapsedMs, int savedProviders)> ProcessProviderResults(
            IEnumerable<ProviderResult> providerResults,
            GenerateAllocationMessageProperties messageProperties, 
            Message message)
        {
            (long saveToCosmosElapsedMs, long saveToSearchElapsedMs, int savedProviders) saveProviderResultsTimings = (-1, -1, -1);

            if (!message.UserProperties.ContainsKey("ignore-save-provider-results"))
            {
                _logger.Information($"Saving results for specification id {messageProperties.SpecificationId}");
                
                saveProviderResultsTimings = await _providerResultsRepositoryPolicy.ExecuteAsync(() => _providerResultsRepository.SaveProviderResults(providerResults, 
                    messageProperties.PartitionIndex, 
                    messageProperties.PartitionSize, 
                    _engineSettings.SaveProviderDegreeOfParallelism));

                _logger.Information($"Saving results completeed for specification id {messageProperties.SpecificationId}");
            }

            // Should just be the GUID as the content, as the prefix is read by the receiver, rather than the sender
            string providerResultsCacheKey = Guid.NewGuid().ToString();

            _logger.Information($"Saving results to cache for specification id {messageProperties.SpecificationId} with key {providerResultsCacheKey}");

            Stopwatch saveRedisStopwatch = Stopwatch.StartNew();
            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync($"{CacheKeys.ProviderResultBatch}{providerResultsCacheKey}", providerResults.ToList(), TimeSpan.FromHours(12), false));
            saveRedisStopwatch.Stop();

            _logger.Information($"Saved results to cache for specification id {messageProperties.SpecificationId} with key {providerResultsCacheKey}");

            IDictionary<string, string> properties = message.BuildMessageProperties();

            properties.Add("specificationId", messageProperties.SpecificationId);

            properties.Add("providerResultsCacheKey", providerResultsCacheKey);

            _logger.Information($"Sending message for test exceution for specification id {messageProperties.SpecificationId}");

            Stopwatch saveQueueStopwatch = Stopwatch.StartNew();
            await _messengerServicePolicy.ExecuteAsync(() => _messengerService.SendToQueue<string>(ServiceBusConstants.QueueNames.TestEngineExecuteTests, null, properties));
            saveQueueStopwatch.Stop();

            _logger.Information($"Message sent for test exceution for specification id {messageProperties.SpecificationId}");

            return (saveProviderResultsTimings.saveToCosmosElapsedMs, 
                saveProviderResultsTimings.saveToSearchElapsedMs, 
                saveRedisStopwatch.ElapsedMilliseconds, 
                saveQueueStopwatch.ElapsedMilliseconds, 
                saveProviderResultsTimings.savedProviders);
        }

        private async Task FailJob(string jobId, int itemsProcessed, string outcome = null)
        {
            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                ItemsProcessed = itemsProcessed,
                Outcome = outcome
            };

            ApiResponse<JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

            if (jobLogResponse == null || jobLogResponse.Content == null)
            {
                _logger.Error($"Failed to add a job log for job id '{jobId}'");
            }
        }
    }
}
