using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.EventHub;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;

namespace CalculateFunding.Services.Results
{
    public class ResultsService : IResultsService
    {
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly ICalculationResultsRepository _resultsRepository;
        private readonly IMapper _mapper;
        private readonly ISearchRepository<ProviderIndex> _searchRepository;
        private readonly IMessengerService _messengerService;
        private readonly EventHubSettings _eventHubSettings;
        private readonly IProviderSourceDatasetRepository _providerSourceDatasetRepository;

        const string ProcessDatasetSubscription = "dataset-events-datasets";

        public ResultsService(ILogger logger,
            ICalculationResultsRepository resultsRepository,
            IMapper mapper,
            ISearchRepository<ProviderIndex> searchRepository,
            IMessengerService messengerService,
            EventHubSettings EventHubSettings,
            ITelemetry telemetry,
            IProviderSourceDatasetRepository providerSourceDatasetRepository)
        {
            _logger = logger;
            _resultsRepository = resultsRepository;
            _mapper = mapper;
            _searchRepository = searchRepository;
            _messengerService = messengerService;
            _eventHubSettings = EventHubSettings;
            _telemetry = telemetry;
            _providerSourceDatasetRepository = providerSourceDatasetRepository;
        }

        public async Task UpdateProviderData(EventData message)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ProviderResult result = message.GetPayloadAsInstanceOf<ProviderResult>();

            if (result == null)
            {
                _logger.Error("Null results provided to UpdateProviderData");
                throw new ArgumentNullException(nameof(result), "Null results provided to UpdateProviderData");
            }

            IEnumerable<ProviderResult> results = new[] { result };

            if (results.Any())
            {
                HttpStatusCode statusCode = await _resultsRepository.UpdateProviderResults(results.ToList());
                stopwatch.Stop();

                if (!statusCode.IsSuccess())
                {
                    _logger.Error($"Failed to bulk update provider data with status code: {statusCode.ToString()}");
                }
                else
                {
                    _telemetry.TrackEvent("UpdateProviderData",
                        new Dictionary<string, string>() {
                            { "CorrelationId", message.GetCorrelationId() }
                        },
                        new Dictionary<string, double>()
                        {
                            { "update-provider-data-elapsedMilliseconds", stopwatch.ElapsedMilliseconds },
                            { "update-provider-data-recordsUpdated", results.Count() },
                        }
                    );
                }

            }
            else
            {
                _logger.Warning("An empty list of results were provided to update");
            }
        }

        public async Task<IActionResult> GetProviderById(HttpRequest request)
        {
            var providerId = GetParameter(request, "providerId");

            if (string.IsNullOrWhiteSpace(providerId))
            {
                _logger.Error("No provider Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty provider Id provided");
            }

            ProviderIndex provider = await _searchRepository.SearchById(providerId, IdFieldOverride: "ukPrn");

            if (provider == null)
                return new NotFoundResult();

            return new OkObjectResult(provider);
        }

        public async Task<IActionResult> GetProviderResults(HttpRequest request)
        {
            var providerId = GetParameter(request, "providerId");
            var specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(providerId))
            {
                _logger.Error("No provider Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty provider Id provided");
            }

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            ProviderResult providerResult = await _resultsRepository.GetProviderResult(providerId, specificationId);

            if (providerResult != null)
            {
                _logger.Information($"A result was found for provider id {providerId}, specification id {specificationId}");

                return new OkObjectResult(providerResult);
            }

            _logger.Information($"A result was not found for provider id {providerId}, specification id {specificationId}");

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetProviderResultsBySpecificationId(HttpRequest request)
        {
            var specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<ProviderResult> providerResults = await _resultsRepository.GetProviderResultsBySpecificationId(specificationId);

            return new OkObjectResult(providerResults);
        }

        public async Task<IActionResult> GetProviderSpecifications(HttpRequest request)
        {
            var providerId = GetParameter(request, "providerId");
            if (string.IsNullOrWhiteSpace(providerId))
            {
                _logger.Error("No provider Id was provided to GetProviderSpecifications");
                return new BadRequestObjectResult("Null or empty provider Id provided");
            }

            IEnumerable<ProviderResult> providerResults = (await _resultsRepository.GetSpecificationResults(providerId)).ToList();

            if (!providerResults.IsNullOrEmpty())
            {
                _logger.Information($"A results was found for provider id {providerId}");

                var specs = providerResults.Where(m => m.Specification != null).Select(m => m.Specification).DistinctBy(m => m.Id).ToList();

                return new OkObjectResult(specs);
            }

            _logger.Information($"Results were not found for provider id {providerId}");

            return new OkObjectResult(Enumerable.Empty<SpecificationSummary>());

        }

        private static string GetParameter(HttpRequest request, string name)
        {
            if (request.Query.TryGetValue(name, out var parameter))
            {
                return parameter.FirstOrDefault();
            }
            return null;
        }

        async public Task<IActionResult> UpdateProviderSourceDataset(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            ProviderSourceDataset sourceDatset = JsonConvert.DeserializeObject<ProviderSourceDataset>(json);

            if (sourceDatset == null)
            {
                _logger.Error("Null results source dataset was provided to UpdateProviderSourceDataset");
                throw new ArgumentNullException(nameof(sourceDatset), "Null results source dataset was provided to UpdateProviderSourceDataset");
            }

            HttpStatusCode statusCode = await _providerSourceDatasetRepository.UpsertProviderSourceDataset(sourceDatset);

            if (!statusCode.IsSuccess())
            {
                int status = (int)statusCode;

                _logger.Error($"Failed to update provider source dataset with status code: {status}");

                return new StatusCodeResult(status);
            }

            return new NoContentResult();
        }

        public async Task<IActionResult> GetProviderSourceDatasetsByProviderIdAndSpecificationId(HttpRequest request)
        {
            var specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResultsBySpecificationId");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            var providerId = GetParameter(request, "providerId");

            if (string.IsNullOrWhiteSpace(providerId))
            {
                _logger.Error("No provider Id was provided to GetProviderResultsBySpecificationId");
                return new BadRequestObjectResult("Null or empty provider Id provided");
            }

            IEnumerable<ProviderSourceDataset> providerResults = await _providerSourceDatasetRepository.GetProviderSourceDatasets(providerId, specificationId);

            return new OkObjectResult(providerResults);
        }
    }
}
