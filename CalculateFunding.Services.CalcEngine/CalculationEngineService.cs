using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.CalcEngine;
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
            IValidator<ICalculatorResiliencePolicies> calculatorResiliencePoliciesValidator)
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

            IEnumerable<ProviderSummary> summaries = null;

            string specificationId = message.UserProperties["specification-id"].ToString();

            CalculationEngineServiceValidator.ValidateMessage(_logger, message);

            BuildProject buildProject = await _calculationsRepository.GetBuildProjectBySpecificationId(specificationId);

            if (buildProject == null)
            {
                _logger.Error("A null build project was provided to UpdateAllocations");

                throw new ArgumentNullException(nameof(buildProject));
            }

            int partitionIndex = int.Parse(message.UserProperties["provider-summaries-partition-index"].ToString());

            int partitionSize = int.Parse(message.UserProperties["provider-summaries-partition-size"].ToString());

            int start = partitionIndex;

            int stop = start + partitionSize - 1;

            string cacheKey = message.UserProperties["provider-cache-key"].ToString();

            summaries = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.ListRangeAsync<ProviderSummary>(cacheKey, start, stop));

            int providerBatchSize = _engineSettings.ProviderBatchSize;

            Stopwatch calculationsLookupStopwatch = Stopwatch.StartNew();
            IEnumerable<CalculationSummaryModel> calculations = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationSummariesForSpecification(specificationId));
            if (calculations == null)
            {
                throw new InvalidOperationException("Calculations lookup API returned null");
            }
            calculationsLookupStopwatch.Stop();

            for (int i = 0; i < summaries.Count(); i += providerBatchSize)
            {
                var calcTiming = Stopwatch.StartNew();

                ConcurrentBag<ProviderResult> providerResults = new ConcurrentBag<ProviderResult>();

                IEnumerable<ProviderSummary> partitionedSummaries = summaries.Skip(i).Take(providerBatchSize);

                IList<string> providerIdList = partitionedSummaries.Select(m => m.Id).ToList();

                Stopwatch providerSourceDatasetsStopwatch = Stopwatch.StartNew();

                List<ProviderSourceDataset> providerSourceDatasets = new List<ProviderSourceDataset>(await _providerSourceDatasetsRepositoryPolicy.ExecuteAsync(() => _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(providerIdList, specificationId)));

                providerSourceDatasetsStopwatch.Stop();

                if (providerSourceDatasets == null)
                {
                    providerSourceDatasets = new List<ProviderSourceDataset>();
                }

                Stopwatch calculationStopwatch = Stopwatch.StartNew();

                Assembly assembly = Assembly.Load(Convert.FromBase64String(buildProject.Build.AssemblyBase64));
                Parallel.ForEach(partitionedSummaries, new ParallelOptions { MaxDegreeOfParallelism = _engineSettings.CalculateProviderResultsDegreeOfParallelism }, provider =>
                {
                    IAllocationModel allocationModel = _calculationEngine.GenerateAllocationModel(assembly);

                    IEnumerable<ProviderSourceDataset> providerDatasets = providerSourceDatasets.Where(m => m.ProviderId == provider.Id);

                    ProviderResult result = _calculationEngine.CalculateProviderResults(allocationModel, buildProject, calculations, provider, providerDatasets);

                    if (result != null)
                    {
                        providerResults.Add(result);
                    }
                    else
                    {
                        throw new InvalidOperationException("Null result from Calc Engine CalculateProviderResults");
                    }
                });

                calculationStopwatch.Stop();

                double? saveCosmosElapsedMs = null;
                double saveRedisElapsedMs = 0;
                double saveQueueElapsedMs = 0;

                if (providerResults.Any())
                {
                    if (!message.UserProperties.ContainsKey("ignore-save-provider-results"))
                    {
                        Stopwatch saveCosmosStopwatch = Stopwatch.StartNew();
                        await _providerResultsRepositoryPolicy.ExecuteAsync(() => _providerResultsRepository.SaveProviderResults(providerResults, _engineSettings.SaveProviderDegreeOfParallelism));
                        saveCosmosStopwatch.Stop();
                        saveCosmosElapsedMs = saveCosmosStopwatch.ElapsedMilliseconds;
                    }

                    // Should just be the GUID as the content, as the prefix is read by the receiver, rather than the sender
                    string providerResultsCacheKey = Guid.NewGuid().ToString();

                    Stopwatch saveRedisStopwatch = Stopwatch.StartNew();
                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync<List<ProviderResult>>($"{CacheKeys.ProviderResultBatch}{providerResultsCacheKey}", providerResults.ToList(), TimeSpan.FromHours(12), false));
                    saveRedisStopwatch.Stop();

                    saveRedisElapsedMs = saveRedisStopwatch.ElapsedMilliseconds;

                    IDictionary<string, string> properties = message.BuildMessageProperties();

                    properties.Add("specificationId", specificationId);

                    properties.Add("providerResultsCacheKey", providerResultsCacheKey);

                    Stopwatch saveQueueStopwatch = Stopwatch.StartNew();
                    await _messengerServicePolicy.ExecuteAsync(() => _messengerService.SendToQueue<string>(ServiceBusConstants.QueueNames.TestEngineExecuteTests, null, properties));
                    saveQueueStopwatch.Stop();

                    saveQueueElapsedMs = saveQueueStopwatch.ElapsedMilliseconds;
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
        }
    }
}
