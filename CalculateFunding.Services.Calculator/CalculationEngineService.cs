using CalculateFunding.Services.Calculator.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.EventHub;
using CalculateFunding.Models.Results;
using System.Diagnostics;
using System.Linq;
using CalculateFunding.Services.Core.Interfaces.Logging;

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

        public CalculationEngineService(
            ILogger logger, 
            ICalculationEngine calculationEngine, 
            ICacheProvider cacheProvider,
            IMessengerService messengerService,
            IProviderSourceDatasetsRepository providerSourceDatasetsRepository,
            ITelemetry telemetry,
            IProviderResultsRepository providerResultsRepository)
        {
            _logger = logger;
            _calculationEngine = calculationEngine;
            _cacheProvider = cacheProvider;
            _messengerService = messengerService;
            _providerSourceDatasetsRepository = providerSourceDatasetsRepository;
            _telemetry = telemetry;
            _providerResultsRepository = providerResultsRepository;
        }

        public async Task GenerateAllocations(EventData message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            BuildProject buildProject = message.GetPayloadAsInstanceOf<BuildProject>();

            string specificationId = buildProject.Specification.Id;

            if (buildProject == null)
            {
                _logger.Error("A null build project was provided to GenrateAllocations");

                throw new ArgumentNullException(nameof(buildProject));
            }

            if (!message.Properties.ContainsKey("provider-summaries-partition-index"))
            {
                _logger.Error("Provider summaries partition index key not found in message properties");

                throw new KeyNotFoundException("Provider summaries partition index key not found in message properties");
            }

            if (!message.Properties.ContainsKey("provider-summaries-partition-size"))
            {
                _logger.Error("Provider summaries partition size key not found in message properties");

                throw new KeyNotFoundException("Provider summaries partition size key not found in message properties");
            }

            int partitionIndex = int.Parse(message.Properties["provider-summaries-partition-index"].ToString());

            int partitionSize = int.Parse(message.Properties["provider-summaries-partition-size"].ToString());

            int start = partitionIndex;

            int stop = start + partitionSize;

            IEnumerable<ProviderSummary> summaries = await _cacheProvider.ListRangeAsync<ProviderSummary>("all-cached-providers", start, stop);

            //if summaries = null, shouldnt be!!, but if is then get from search

            IAllocationModel allocationModel = _calculationEngine.GenerateAllocationModel(buildProject);

            for (int i = 0; i < summaries.Count(); i += 100)
            {
                var calcTiming = Stopwatch.StartNew();

                IList<ProviderResult> providerResults = new List<ProviderResult>();

                IEnumerable<ProviderSummary> partitionedSummaries = summaries.Skip(i).Take(100);

                IList<string> providerIdList = partitionedSummaries.Select(m => m.Id).ToList();

                IEnumerable<ProviderSourceDataset> providerSourceDatasets = await _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(providerIdList, specificationId);

                if (providerSourceDatasets == null)
                {
                    providerSourceDatasets = Enumerable.Empty<ProviderSourceDataset>();
                }

                Parallel.ForEach(partitionedSummaries, new ParallelOptions { MaxDegreeOfParallelism = 5 }, provider =>
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    IEnumerable<ProviderSourceDataset> providerDatasets = providerSourceDatasets.Where(m => m.Provider?.Id == provider.Id);

                    var result = _calculationEngine.CalculateProviderResults(allocationModel, buildProject, provider, providerDatasets);

                    if (result != null)
                        providerResults.Add(result);

                    stopwatch.Stop();

                    _logger.Debug($"Generated result for {provider.Name} in {stopwatch.ElapsedMilliseconds}ms");
                });

                if (providerResults.Any())
                {
                    await _providerResultsRepository.SaveProviderResults(providerResults);

                    string providerResultsCacheKey = Guid.NewGuid().ToString();

                    await _cacheProvider.SetAsync<List<ProviderResult>>(providerResultsCacheKey, providerResults.ToList(), TimeSpan.FromHours(12), false);

                    IDictionary<string, string> properties = message.BuildMessageProperties();

                    properties.Add("specificationId", specificationId);

                    properties.Add("providerResultsCacheKey", providerResultsCacheKey);

                    await _messengerService.SendAsync(ExecuteTestsEventSubscription, buildProject, properties);
                }

                calcTiming.Stop();

                _telemetry.TrackEvent("CalculationRunProvidersProcessed",
                    new Dictionary<string, string>()
                    {
                    { "specificationId" , specificationId },
                    { "buildProjectId" , buildProject.Id }
                    },
                    new Dictionary<string, double>()
                    {
                    { "calculation-run-providersProcessed", partitionedSummaries.Count() }
                    }
                );
            }
        }
    }
}
