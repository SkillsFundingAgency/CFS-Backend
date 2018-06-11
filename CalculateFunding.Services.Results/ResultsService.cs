using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;
using CalculateFunding.Repositories.Common.Cosmos;
using Polly;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Models;
using CalculateFunding.Services.Results.ResultModels;

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
        private readonly ServiceBusSettings _eventHubSettings;
        private readonly IProviderSourceDatasetRepository _providerSourceDatasetRepository;
        private readonly ISearchRepository<CalculationProviderResultsIndex> _calculationProviderResultsSearchRepository;
        private readonly Polly.Policy _resultsRepositoryPolicy;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly Polly.Policy _resultsSearchRepositoryPolicy;
        private readonly Polly.Policy _specificationsRepositoryPolicy;
        private readonly IPublishedProviderResultsAssemblerService _publishedProviderResultsAssemblerService;
        private readonly IPublishedProviderResultsRepository _publishedProviderResultsRepository;

        const string ProcessDatasetSubscription = "dataset-events-datasets";

        public ResultsService(ILogger logger,
            ICalculationResultsRepository resultsRepository,
            IMapper mapper,
            ISearchRepository<ProviderIndex> searchRepository,
            IMessengerService messengerService,
            ServiceBusSettings EventHubSettings,
            ITelemetry telemetry,
            IProviderSourceDatasetRepository providerSourceDatasetRepository,
            ISearchRepository<CalculationProviderResultsIndex> calculationProviderResultsSearchRepository,
            ISpecificationsRepository specificationsRepository,
            IResultsResilliencePolicies resiliencePolicies,
            IPublishedProviderResultsAssemblerService publishedProviderResultsAssemblerService,
            IPublishedProviderResultsRepository publishedProviderResultsRepository)
        {
            _logger = logger;
            _resultsRepository = resultsRepository;
            _mapper = mapper;
            _searchRepository = searchRepository;
            _messengerService = messengerService;
            _eventHubSettings = EventHubSettings;
            _telemetry = telemetry;
            _providerSourceDatasetRepository = providerSourceDatasetRepository;
            _calculationProviderResultsSearchRepository = calculationProviderResultsSearchRepository;
            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _specificationsRepository = specificationsRepository;
            _resultsSearchRepositoryPolicy = resiliencePolicies.ResultsSearchRepository;
            _specificationsRepositoryPolicy = resiliencePolicies.SpecificationsRepository;
            _publishedProviderResultsAssemblerService = publishedProviderResultsAssemblerService;
            _publishedProviderResultsRepository = publishedProviderResultsRepository;
        }

        public async Task UpdateProviderData(Message message)
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
                HttpStatusCode statusCode = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.UpdateProviderResults(results.ToList()));
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

            ProviderIndex provider = await _resultsRepositoryPolicy.ExecuteAsync(() => _searchRepository.SearchById(providerId, IdFieldOverride: "ukPrn"));

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

            ProviderResult providerResult = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetProviderResult(providerId, specificationId));

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

            IEnumerable<ProviderResult> providerResults = await GetProviderResultsBySpecificationId(specificationId);

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

            // Returns distinct specificationIds where there are results for this provider
            List<string> result = new List<string>();

            IEnumerable<ProviderResult> providerResults = (await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetSpecificationResults(providerId))).ToList();

            if (!providerResults.IsNullOrEmpty())
            {
                _logger.Information($"Results was found for provider id {providerId}");

                result.AddRange(providerResults.Where(m => !string.IsNullOrWhiteSpace(m.SpecificationId)).Select(s => s.SpecificationId).Distinct());
            }
            else
            {
                _logger.Information($"Results were not found for provider id '{providerId}'");
            }

            return new OkObjectResult(result);

        }

        async public Task<IActionResult> GetFundingCalculationResultsForSpecifications(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SpecificationListModel specifications = JsonConvert.DeserializeObject<SpecificationListModel>(json);

            if (specifications == null)
            {
                _logger.Error("Null specification model provided");

                return new BadRequestObjectResult("Null specifications model provided");
            }

            if (specifications.SpecificationIds.IsNullOrEmpty())
            {
                _logger.Error("Null or empty specification ids provided");

                return new BadRequestObjectResult("Null or empty specification ids provided");
            }

            IList<FundingCalculationResultsTotals> totalsModels = new List<FundingCalculationResultsTotals>();

            IList<Task> totalsTasks = new List<Task>();

            foreach (string specificationId in specifications.SpecificationIds)
            {
                totalsTasks.Add(Task.Run(async () =>
                {
                    decimal totalResult = await _resultsRepository.GetCalculationResultTotalForSpecificationId(specificationId);

                    totalsModels.Add(new FundingCalculationResultsTotals
                    {
                        SpecificationId = specificationId,
                        TotalResult = totalResult
                    });

                }));
            }

            try
            {
                await TaskHelper.WhenAllAndThrow(totalsTasks.ToArray());
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult($"An error occurred when obtaining calculation totals with the follwing message: \n {ex.Message}");
            }

            return new OkObjectResult(totalsModels);
        }

        public async Task<IActionResult> PublishProviderResults(HttpRequest request)
        {
            string specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<ProviderResult> providerResults = await GetProviderResultsBySpecificationId(specificationId);

            if (providerResults.IsNullOrEmpty())
            {
                _logger.Error($"Provider results not found for specification id {specificationId}");

                return new NotFoundObjectResult($"Provider results not found");
            }

            Reference author = request.GetUser();

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsAssemblerService.Assemble(providerResults, author, specificationId);

            try
            {
                await _publishedProviderResultsRepository.CreatePublishedResults(publishedProviderResults.ToList());
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to create published provider results for specification: {specificationId}");

                return new InternalServerErrorResult("Failed to create published provider results");
            }

            return new NoContentResult();
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

            HttpStatusCode statusCode = await _resultsRepositoryPolicy.ExecuteAsync(() => _providerSourceDatasetRepository.UpsertProviderSourceDataset(sourceDatset));

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

            IEnumerable<ProviderSourceDataset> providerResults = await _resultsRepositoryPolicy.ExecuteAsync(() => _providerSourceDatasetRepository.GetProviderSourceDatasets(providerId, specificationId));

            return new OkObjectResult(providerResults);
        }

        public async Task<IActionResult> GetScopedProviderIdsBySpecificationId(HttpRequest request)
        {
            var specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResultsBySpecificationId");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<string> providerResults = await _resultsRepositoryPolicy.ExecuteAsync(() => _providerSourceDatasetRepository.GetAllScopedProviderIdsForSpecificationid(specificationId));

            return new OkObjectResult(providerResults);
        }

        public async Task<IActionResult> ReIndexCalculationProviderResults()
        {
            IEnumerable<DocumentEntity<ProviderResult>> providerResults = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetAllProviderResults());

            IList<CalculationProviderResultsIndex> searchItems = new List<CalculationProviderResultsIndex>();

            Dictionary<string, SpecificationSummary> specifications = new Dictionary<string, SpecificationSummary>();

            foreach (DocumentEntity<ProviderResult> documentEnity in providerResults)
            {
                ProviderResult providerResult = documentEnity.Content;

                foreach (CalculationResult calculationResult in providerResult.CalculationResults)
                {
                    if (calculationResult.Value.HasValue)
                    {
                        SpecificationSummary specificationSummary = null;
                        if (!specifications.ContainsKey(providerResult.SpecificationId))
                        {
                            specificationSummary = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetSpecificationSummaryById(providerResult.SpecificationId));
                            if(specificationSummary == null)
                            {
                                throw new InvalidOperationException($"Specification Summary returned null for specification ID '{providerResult.SpecificationId}'");
                            }

                            specifications.Add(providerResult.SpecificationId, specificationSummary);
                        }
                        else
                        {
                            specificationSummary = specifications[providerResult.SpecificationId];
                        }

                        searchItems.Add(new CalculationProviderResultsIndex
                        {
                            SpecificationId = providerResult.SpecificationId,
                            SpecificationName = specificationSummary?.Name,
                            CalculationSpecificationId = calculationResult.CalculationSpecification.Id,
                            CalculationSpecificationName = calculationResult.CalculationSpecification.Name,
                            CalculationName = calculationResult.Calculation.Name,
                            CalculationId = calculationResult.Calculation.Id,
                            CalculationType = calculationResult.CalculationType.ToString(),
                            ProviderId = providerResult.Provider.Id,
                            ProviderName = providerResult.Provider.Name,
                            ProviderType = providerResult.Provider.ProviderType,
                            ProviderSubType = providerResult.Provider.ProviderSubType,
                            LocalAuthority = providerResult.Provider.Authority,
                            LastUpdatedDate = documentEnity.UpdatedAt,
                            UKPRN = providerResult.Provider.UKPRN,
                            URN = providerResult.Provider.URN,
                            UPIN = providerResult.Provider.UPIN,
                            EstablishmentNumber = providerResult.Provider.EstablishmentNumber,
                            OpenDate = providerResult.Provider.DateOpened,
                            CaclulationResult = calculationResult.Value.HasValue ? Convert.ToDouble(calculationResult.Value) : 0
                        });
                    }
                }
            }

            for (int i = 0; i < searchItems.Count; i += 500)
            {
                IEnumerable<CalculationProviderResultsIndex> partitionedResults = searchItems.Skip(i).Take(500);

                IEnumerable<IndexError> errors = await _resultsSearchRepositoryPolicy.ExecuteAsync(() => _calculationProviderResultsSearchRepository.Index(partitionedResults));

                if (errors.Any())
                {
                    _logger.Error($"Failed to index calculation provider result documents with errors: { string.Join(";", errors.Select(m => m.ErrorMessage)) }");

                    return new StatusCodeResult(500);
                }
            }

            return new NoContentResult();
        }

        public async Task<IActionResult> GetPublishedProviderResultsBySpecificationId(HttpRequest request)
        {
            var specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetPublishedProviderResultsBySpecificationId");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepository.GetPublishedProviderResultsForSpecificationId(specificationId);

            IEnumerable<PublishedProviderResultModel> publishedProviderResultModels = MapPublishedProviderResultModels(publishedProviderResults);

            return new OkObjectResult(publishedProviderResultModels);
        }

        private static string GetParameter(HttpRequest request, string name)
        {
            if (request.Query.TryGetValue(name, out var parameter))
            {
                return parameter.FirstOrDefault();
            }
            return null;
        }

        public Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId)
        {
            return _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetProviderResultsBySpecificationId(specificationId));
        }

        IEnumerable<PublishedProviderResultModel> MapPublishedProviderResultModels(IEnumerable<PublishedProviderResult> publishedProviderResults)
        {
            if (publishedProviderResults.IsNullOrEmpty())
            {
                return Enumerable.Empty<PublishedProviderResultModel>();
            }

            IList<PublishedProviderResultModel> publishedProviderResultModels = new List<PublishedProviderResultModel>();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                PublishedProviderResultModel publishedProviderResultModel = new PublishedProviderResultModel
                {
                    ProviderId = publishedProviderResult.ProviderId,
                    ProviderName = publishedProviderResult.Name,
                    Ukprn = publishedProviderResult.Ukprn,
                    SpecificationId = publishedProviderResult.SpecificationId,
                    FundingStreamResults = publishedProviderResult.FundingStreamResults.Select(
                        m => new PublishedFundingStreamResultModel
                        {
                            FundingStreamId = m.FundingStream.Id,
                            FundingStreamName = m.FundingStream.Name,
                            AllocationLineResults = m.AllocationLineResults.IsNullOrEmpty() ? Enumerable.Empty<PublishedAllocationLineResultModel>() : m.AllocationLineResults.Select(
                                    alr => new PublishedAllocationLineResultModel
                                    {
                                        AllocationLineId = alr.AllocationLine.Id,
                                        AllocationLineName = alr.AllocationLine.Name,
                                        FundingAmount = alr.Value,
                                        Status = alr.Current.Status,
                                        LastUpdated = alr.Current.Date
                                    }

                                )

                        })
                };

                publishedProviderResultModels.Add(publishedProviderResultModel);
            }

            return publishedProviderResultModels;
        }
    }
}
