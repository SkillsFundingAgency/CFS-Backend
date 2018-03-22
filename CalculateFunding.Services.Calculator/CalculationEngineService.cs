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

        private readonly ILogger _logger;
        private readonly ICalculationEngine _calculationEngine;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMessengerService _messengerService;
        private readonly IProviderSourceDatasetsRepository _providerSourceDatasetsRepository;
        private readonly ITelemetry _telemetry;

        public CalculationEngineService(
            ILogger logger, 
            ICalculationEngine calculationEngine, 
            ICacheProvider cacheProvider,
            IMessengerService messengerService,
            IProviderSourceDatasetsRepository providerSourceDatasetsRepository,
            ITelemetry telemetry)
        {
            _logger = logger;
            _calculationEngine = calculationEngine;
            _cacheProvider = cacheProvider;
            _messengerService = messengerService;
            _providerSourceDatasetsRepository = providerSourceDatasetsRepository;
            _telemetry = telemetry;
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

            if (!message.Properties.ContainsKey("provider-summaries-cache-key"))
            {
                _logger.Error("Provider summaries cache key not found in message properties");

                throw new KeyNotFoundException("Provider summaries cache key not found in message properties");
            }

            IEnumerable<ProviderSummary> summaries = await _cacheProvider.GetAsync<List<ProviderSummary>>(message.Properties["provider-summaries-cache-key"].ToString());

            //if summaries = null, shouldnt be!!, but if is then get from search

            IList<ProviderResult> providerResults = new List<ProviderResult>();

            IAllocationModel allocationModel = _calculationEngine.GenerateAllocationModel(buildProject);

            var calcTiming = Stopwatch.StartNew();
            
            Parallel.ForEach(summaries, new ParallelOptions { MaxDegreeOfParallelism = 5 }, provider =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                IEnumerable<ProviderSourceDataset> providerSourceDatasets = _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdAndSpecificationId(provider.Id, buildProject.Specification.Id).Result;

                if (providerSourceDatasets == null)
                {
                    providerSourceDatasets = Enumerable.Empty<ProviderSourceDataset>();
                }

                var result = _calculationEngine.CalculateProviderResults(allocationModel, buildProject, provider, providerSourceDatasets.ToList());

                if(result != null)
                    providerResults.Add(result);

                stopwatch.Stop();

                _logger.Information($"Generated result for {provider.Name} in {stopwatch.ElapsedMilliseconds}ms");
            });

            if (providerResults.Any())
            {
                IDictionary<string, string> properties = message.BuildMessageProperties();

                await _messengerService.SendAsync(UpdateCosmosResultsCollection, providerResults, properties);
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
                        { "calculation-run-providersProcessed", summaries.Count() }
                }
            );

        }
    }
}
