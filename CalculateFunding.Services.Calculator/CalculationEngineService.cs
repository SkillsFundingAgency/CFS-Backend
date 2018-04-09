using CalculateFunding.Services.Calculator.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Models.Results;
using System.Diagnostics;
using System.Linq;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Options;

namespace CalculateFunding.Services.Calculator
{
    public class CalculationEngineService : ICalculationEngineService
    {
        const string UpdateCosmosResultsCollection = "dataset-events-results";

        const string ExecuteTestsEventSubscription = "test-events-execute-tests";

        private readonly ILogger _logger;
        private readonly ICalculationEngine _calculationEngine;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMessengerService _messengerService;
        private readonly IProviderSourceDatasetsRepository _providerSourceDatasetsRepository;
        private readonly ITelemetry _telemetry;
        private readonly IProviderResultsRepository _providerResultsRepository;
        private readonly EngineSettings _engineSettings;

        public CalculationEngineService(
            ILogger logger,
            ICalculationEngine calculationEngine,
            ICacheProvider cacheProvider,
            IMessengerService messengerService,
            IProviderSourceDatasetsRepository providerSourceDatasetsRepository,
            ITelemetry telemetry,
            IProviderResultsRepository providerResultsRepository,
            EngineSettings engineSettings)
        {
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));

            _logger = logger;
            _calculationEngine = calculationEngine;
            _cacheProvider = cacheProvider;
            _messengerService = messengerService;
            _providerSourceDatasetsRepository = providerSourceDatasetsRepository;
            _telemetry = telemetry;
            _providerResultsRepository = providerResultsRepository;
            _engineSettings = engineSettings;
        }

        public async Task GenerateAllocations(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            
            BuildProject buildProject = message.GetPayloadAsInstanceOf<BuildProject>();

            string specificationId = buildProject.Specification.Id;

            if (buildProject == null)
            {
                _logger.Error("A null build project was provided to GenrateAllocations");

                throw new ArgumentNullException(nameof(buildProject));
            }

            if (!message.UserProperties.ContainsKey("provider-summaries-partition-index"))
            {
                _logger.Error("Provider summaries partition index key not found in message properties");

                throw new KeyNotFoundException("Provider summaries partition index key not found in message properties");
            }

            if (!message.UserProperties.ContainsKey("provider-summaries-partition-size"))
            {
                _logger.Error("Provider summaries partition size key not found in message properties");

                throw new KeyNotFoundException("Provider summaries partition size key not found in message properties");
            }

            int partitionIndex = int.Parse(message.UserProperties["provider-summaries-partition-index"].ToString());

            int partitionSize = int.Parse(message.UserProperties["provider-summaries-partition-size"].ToString());
            if (partitionSize <= 0)
            {
                _logger.Error("Partition size is zero or less. {partitionSize}", partitionSize);

                throw new KeyNotFoundException($"Partition size is zero or less. {partitionSize}");
            }

            int start = partitionIndex;

            int stop = start + partitionSize - 1;

            IEnumerable<ProviderSummary> summaries = await _cacheProvider.ListRangeAsync<ProviderSummary>("all-cached-providers", start, stop);

            //if summaries = null, shouldnt be!!, but if is then get from search

            IAllocationModel allocationModel = _calculationEngine.GenerateAllocationModel(buildProject);

            int providerBatchSize = _engineSettings.ProviderBatchSize;

            for (int i = 0; i < summaries.Count(); i += providerBatchSize)
            {
                var calcTiming = Stopwatch.StartNew();

                IList<ProviderResult> providerResults = new List<ProviderResult>();

                IEnumerable<ProviderSummary> partitionedSummaries = summaries.Skip(i).Take(providerBatchSize);

                IList<string> providerIdList = partitionedSummaries.Select(m => m.Id).ToList();

                Stopwatch providerSourceDatasetsStopwatch = Stopwatch.StartNew();
                // Convert to list to ensure no deferred execution in cosmos
                List<ProviderSourceDataset> providerSourceDatasets = new List<ProviderSourceDataset>(await _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(providerIdList, specificationId));
                providerSourceDatasetsStopwatch.Stop();

                if (providerSourceDatasets == null)
                {
                    providerSourceDatasets = new List<ProviderSourceDataset>();
                }

                Stopwatch calculationStopwatch = Stopwatch.StartNew();
                Parallel.ForEach(partitionedSummaries, new ParallelOptions { MaxDegreeOfParallelism = _engineSettings.CalculateProviderResultsDegreeOfParallelism }, provider =>
                {
                    IEnumerable<ProviderSourceDataset> providerDatasets = providerSourceDatasets.Where(m => m.Provider?.Id == provider.Id);

                    var result = _calculationEngine.CalculateProviderResults(allocationModel, buildProject, provider, providerDatasets);

                    if (result != null)
                        providerResults.Add(result);
                });
                calculationStopwatch.Stop();

                double saveCosmosElapsedMs = 0;
                double saveRedisElapsedMs = 0;
                double saveQueueElapsedMs = 0;

                if (providerResults.Any())
                {
                    Stopwatch saveCosmosStopwatch = Stopwatch.StartNew();
                    await _providerResultsRepository.SaveProviderResults(providerResults, _engineSettings.SaveProviderDegreeOfParallelism);
                    saveCosmosStopwatch.Stop();

                    saveCosmosElapsedMs = saveCosmosStopwatch.ElapsedMilliseconds;

                    string providerResultsCacheKey = Guid.NewGuid().ToString();

                    Stopwatch saveRedisStopwatch = Stopwatch.StartNew();
                    await _cacheProvider.SetAsync<List<ProviderResult>>(providerResultsCacheKey, providerResults.ToList(), TimeSpan.FromHours(12), false);
                    saveRedisStopwatch.Stop();

                    saveRedisElapsedMs = saveRedisStopwatch.ElapsedMilliseconds;

                    IDictionary<string, string> properties = message.BuildMessageProperties();

                    properties.Add("specificationId", specificationId);

                    properties.Add("providerResultsCacheKey", providerResultsCacheKey);

                    Stopwatch saveQueueStopwatch = Stopwatch.StartNew();
                    await _messengerService.SendToQueue(ExecuteTestsEventSubscription, buildProject, properties);
                    saveQueueStopwatch.Stop();

                    saveQueueElapsedMs = saveQueueStopwatch.ElapsedMilliseconds;
                }

                calcTiming.Stop();

                _telemetry.TrackEvent("CalculationRunProvidersProcessed",
                    new Dictionary<string, string>()
                    {
                        { "specificationId" , specificationId },
                        { "buildProjectId" , buildProject.Id },

                    },
                    new Dictionary<string, double>()
                    {
                        { "calculation-run-providersProcessed", partitionedSummaries.Count() },
                        { "calculation-run-elapsedMilliseconds", calcTiming.ElapsedMilliseconds },
                        { "calculation-run-providersResultsFromCache", summaries.Count() },
                        { "calculation-run-partitionSize", partitionSize },
                        { "calculation-run-providerSourceDatasetQueryMs", providerSourceDatasetsStopwatch.ElapsedMilliseconds },
                        { "calculation-run-saveProviderResultsCosmosMs", saveCosmosElapsedMs },
                        { "calculation-run-saveProviderResultsRedisMs", saveRedisElapsedMs },
                        { "calculation-run-saveProviderResultsServiceBusMs", saveQueueElapsedMs },
                        { "calculation-run-runningCalculationMs", calculationStopwatch.ElapsedMilliseconds },
                    }
                );
            }
        }
    }
}
