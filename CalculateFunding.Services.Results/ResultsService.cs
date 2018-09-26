using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.ResultModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class ResultsService : IResultsService, IHealthChecker
    {

        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly ICalculationResultsRepository _resultsRepository;
        private readonly IMapper _mapper;
        private readonly ISearchRepository<ProviderIndex> _searchRepository;
        private readonly IProviderSourceDatasetRepository _providerSourceDatasetRepository;
        private readonly ISearchRepository<CalculationProviderResultsIndex> _calculationProviderResultsSearchRepository;
        private readonly Polly.Policy _resultsRepositoryPolicy;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly Polly.Policy _resultsSearchRepositoryPolicy;
        private readonly Polly.Policy _specificationsRepositoryPolicy;
        private readonly IPublishedProviderResultsAssemblerService _publishedProviderResultsAssemblerService;
        private readonly IPublishedProviderResultsRepository _publishedProviderResultsRepository;
        private readonly IPublishedProviderCalculationResultsRepository _publishedProviderCalculationResultsRepository;
        private readonly IProviderImportMappingService _providerImportMappingService;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISearchRepository<AllocationNotificationFeedIndex> _allocationNotificationsSearchRepository;
        private readonly Polly.Policy _allocationNotificationsSearchRepositoryPolicy;
        private readonly IProviderProfilingRepository _providerProfilingRepository;
        private readonly Polly.Policy _providerProfilingRepositoryPolicy;
        private readonly Polly.Policy _publishedProviderCalculationResultsRepositoryPolicy;
        private readonly Polly.Policy _publishedProviderResultsRepositoryPolicy;
        private readonly IMessengerService _messengerService;

        public ResultsService(ILogger logger,
            ICalculationResultsRepository resultsRepository,
            IMapper mapper,
            ISearchRepository<ProviderIndex> searchRepository,
            ITelemetry telemetry,
            IProviderSourceDatasetRepository providerSourceDatasetRepository,
            ISearchRepository<CalculationProviderResultsIndex> calculationProviderResultsSearchRepository,
            ISpecificationsRepository specificationsRepository,
            IResultsResilliencePolicies resiliencePolicies,
            IPublishedProviderResultsAssemblerService publishedProviderResultsAssemblerService,
            IPublishedProviderResultsRepository publishedProviderResultsRepository,
            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository,
            IProviderImportMappingService providerImportMappingService,
            ICacheProvider cacheProvider,
            ISearchRepository<AllocationNotificationFeedIndex> allocationNotificationsSearchRepository,
            IProviderProfilingRepository providerProfilingRepository,
            IMessengerService messengerService)
        {
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(providerSourceDatasetRepository, nameof(providerSourceDatasetRepository));
            Guard.ArgumentNotNull(calculationProviderResultsSearchRepository, nameof(calculationProviderResultsSearchRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(publishedProviderResultsAssemblerService, nameof(publishedProviderResultsAssemblerService));
            Guard.ArgumentNotNull(publishedProviderResultsRepository, nameof(publishedProviderResultsRepository));
            Guard.ArgumentNotNull(publishedProviderCalculationResultsRepository, nameof(publishedProviderCalculationResultsRepository));
            Guard.ArgumentNotNull(providerImportMappingService, nameof(providerImportMappingService));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(allocationNotificationsSearchRepository, nameof(allocationNotificationsSearchRepository));
            Guard.ArgumentNotNull(providerProfilingRepository, nameof(providerProfilingRepository));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));

            _logger = logger;
            _resultsRepository = resultsRepository;
            _mapper = mapper;
            _searchRepository = searchRepository;
            _telemetry = telemetry;
            _providerSourceDatasetRepository = providerSourceDatasetRepository;
            _calculationProviderResultsSearchRepository = calculationProviderResultsSearchRepository;
            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _specificationsRepository = specificationsRepository;
            _resultsSearchRepositoryPolicy = resiliencePolicies.ResultsSearchRepository;
            _specificationsRepositoryPolicy = resiliencePolicies.SpecificationsRepository;
            _publishedProviderResultsAssemblerService = publishedProviderResultsAssemblerService;
            _publishedProviderResultsRepository = publishedProviderResultsRepository;
            _publishedProviderCalculationResultsRepository = publishedProviderCalculationResultsRepository;
            _providerImportMappingService = providerImportMappingService;
            _cacheProvider = cacheProvider;
            _allocationNotificationsSearchRepository = allocationNotificationsSearchRepository;
            _allocationNotificationsSearchRepositoryPolicy = resiliencePolicies.AllocationNotificationFeedSearchRepository;
            _publishedProviderCalculationResultsRepositoryPolicy = resiliencePolicies.PublishedProviderCalculationResultsRepository;
            _publishedProviderResultsRepositoryPolicy = resiliencePolicies.PublishedProviderResultsRepository;
            _providerProfilingRepositoryPolicy = resiliencePolicies.ProviderProfilingRepository;
            _providerProfilingRepository = providerProfilingRepository;
            _messengerService = messengerService;
        }

        public ResultsService(ILogger logger,
          ICalculationResultsRepository resultsRepository,
          IMapper mapper,
          ISearchRepository<ProviderIndex> searchRepository,
          ITelemetry telemetry,
          IProviderSourceDatasetRepository providerSourceDatasetRepository,
          ISearchRepository<CalculationProviderResultsIndex> calculationProviderResultsSearchRepository,
          ISpecificationsRepository specificationsRepository,
          IResultsResilliencePolicies resiliencePolicies,
          IPublishedProviderResultsAssemblerService publishedProviderResultsAssemblerService,
          IPublishedProviderResultsRepository publishedProviderResultsRepository,
          IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository,
          IProviderImportMappingService providerImportMappingService,
          ICacheProvider cacheProvider,
          ISearchRepository<AllocationNotificationFeedIndex> allocationNotificationsSearchRepository,
          IMessengerService messengerService)
        {
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(providerSourceDatasetRepository, nameof(providerSourceDatasetRepository));
            Guard.ArgumentNotNull(calculationProviderResultsSearchRepository, nameof(calculationProviderResultsSearchRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(publishedProviderResultsAssemblerService, nameof(publishedProviderResultsAssemblerService));
            Guard.ArgumentNotNull(publishedProviderResultsRepository, nameof(publishedProviderResultsRepository));
            Guard.ArgumentNotNull(publishedProviderCalculationResultsRepository, nameof(publishedProviderCalculationResultsRepository));
            Guard.ArgumentNotNull(providerImportMappingService, nameof(providerImportMappingService));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(allocationNotificationsSearchRepository, nameof(allocationNotificationsSearchRepository));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));

            _logger = logger;
            _resultsRepository = resultsRepository;
            _mapper = mapper;
            _searchRepository = searchRepository;
            _telemetry = telemetry;
            _providerSourceDatasetRepository = providerSourceDatasetRepository;
            _calculationProviderResultsSearchRepository = calculationProviderResultsSearchRepository;
            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _specificationsRepository = specificationsRepository;
            _resultsSearchRepositoryPolicy = resiliencePolicies.ResultsSearchRepository;
            _specificationsRepositoryPolicy = resiliencePolicies.SpecificationsRepository;
            _publishedProviderResultsAssemblerService = publishedProviderResultsAssemblerService;
            _publishedProviderResultsRepository = publishedProviderResultsRepository;
            _publishedProviderCalculationResultsRepository = publishedProviderCalculationResultsRepository;
            _providerImportMappingService = providerImportMappingService;
            _cacheProvider = cacheProvider;
            _allocationNotificationsSearchRepository = allocationNotificationsSearchRepository;
            _allocationNotificationsSearchRepositoryPolicy = resiliencePolicies.AllocationNotificationFeedSearchRepository;
            _publishedProviderCalculationResultsRepositoryPolicy = resiliencePolicies.PublishedProviderCalculationResultsRepository;
            _publishedProviderResultsRepositoryPolicy = resiliencePolicies.PublishedProviderResultsRepository;
            _messengerService = messengerService;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker)_resultsRepository).IsHealthOk();
            var searchRepoHealth = await _searchRepository.IsHealthOk();
            ServiceHealth providerSourceDatasetRepoHealth = await ((IHealthChecker)_providerSourceDatasetRepository).IsHealthOk();
            var calcSearchRepoHealth = await _calculationProviderResultsSearchRepository.IsHealthOk();
            ServiceHealth providerRepoHealth = await ((IHealthChecker)_publishedProviderResultsRepository).IsHealthOk();
            var cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ResultsService)
            };
            health.Dependencies.AddRange(datasetsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });
            health.Dependencies.AddRange(providerSourceDatasetRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = calcSearchRepoHealth.Ok, DependencyName = _calculationProviderResultsSearchRepository.GetType().GetFriendlyName(), Message = calcSearchRepoHealth.Message });
            health.Dependencies.AddRange(providerRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });

            return health;
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
                _logger.Error("No provider Id was provided to GetProviderById");
                return new BadRequestObjectResult("Null or empty provider Id provided");
            }

            ProviderIndex provider = await _resultsRepositoryPolicy.ExecuteAsync(() => _searchRepository.SearchById(providerId, IdFieldOverride: "providerId"));

            if (provider == null)
            {
                return new NotFoundResult();
            }

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

        public async Task<PublishedProviderResult> GetPublishedProviderResultByAllocationResultId(string allocationResultId, int? version = null)
        {
            Guard.IsNullOrWhiteSpace(allocationResultId, nameof(allocationResultId));

            PublishedProviderResult publishedProviderResult = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultForIdInPublishedState(allocationResultId));

            if (publishedProviderResult == null)
            {
                return null;
            }

            if (version.HasValue && version.Value != publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Version)
            {
                PublishedAllocationLineResultHistory history = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedAllocationLineResultHistoryForId(allocationResultId));

                if (history == null)
                {
                    return null;
                }

                PublishedAllocationLineResultVersion resultVersion = history.History.FirstOrDefault(m => m.Version == version.Value);

                if (resultVersion == null)
                {
                    return null;
                }

                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current = resultVersion;
            }

            return publishedProviderResult;
        }

        public async Task<PublishedProviderResult> GetPublishedProviderResultWithHistoryByAllocationResultId(string allocationResultId)
        {
            Guard.IsNullOrWhiteSpace(allocationResultId, nameof(allocationResultId));

            PublishedProviderResult publishedProviderResult = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultForIdInPublishedState(allocationResultId));

            if (publishedProviderResult == null)
            {
                return null;
            }

            PublishedAllocationLineResultHistory history = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedAllocationLineResultHistoryForId(allocationResultId));

            if (history == null)
            {
                return null;
            }

            publishedProviderResult.FundingStreamResult.AllocationLineResult.History = history.History.ToList();

            return publishedProviderResult;
        }

        public async Task<IActionResult> GetProviderResultsBySpecificationId(HttpRequest request)
        {
            var specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResults");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<ProviderResult> providerResults = null;

            string top = GetParameter(request, "top");

            if (!string.IsNullOrWhiteSpace(top))
            {
                int maxResults;

                if (int.TryParse(top, out maxResults))
                {
                    providerResults = await GetProviderResultsBySpecificationId(specificationId, maxResults);

                    return new OkObjectResult(providerResults);
                }
            }

            providerResults = await GetProviderResultsBySpecificationId(specificationId);

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

            ConcurrentBag<FundingCalculationResultsTotals> totalsModels = new ConcurrentBag<FundingCalculationResultsTotals>();

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

        public async Task PublishProviderResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("No specification Id was provided to PublishProviderResults");
                throw new ArgumentException("Message must contain a specification id");
            }

            int calculationProgress = 0;

            string specificationId = message.UserProperties["specification-id"].ToString();
            UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.InProgress);

            IEnumerable<ProviderResult> providerResults = await GetProviderResultsBySpecificationId(specificationId);
            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);

            if (providerResults.IsNullOrEmpty())
            {
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error);
                _logger.Error($"Provider results not found for specification id {specificationId}");
                throw new ArgumentException("Could not find any provider results for specification");
            }

            SpecificationCurrentVersion specification = await _specificationsRepository.GetCurrentSpecificationById(specificationId);
            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);

            if (specification == null)
            {
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error);
                _logger.Error($"Specification not found for specification id {specificationId}");
                throw new ArgumentException($"Specification not found for specification id {specificationId}");
            }

            Reference author = message.GetUserDetails();

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalcuationResults = _publishedProviderResultsAssemblerService.AssemblePublishedCalculationResults(providerResults, author, specification);
            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);

            try
            {
                await _publishedProviderCalculationResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderCalculationResultsRepository.CreatePublishedCalculationResults(publishedProviderCalcuationResults.ToList()));
                UpdateCacheForSegmentDone(specificationId, calculationProgress += 7, CalculationProgressStatus.InProgress);

                await SavePublishedCalculationResultVersionHistory(publishedProviderCalcuationResults, specificationId);
                UpdateCacheForSegmentDone(specificationId, calculationProgress += 10, CalculationProgressStatus.InProgress);
            }
            catch (Exception ex)
            {
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error);
                _logger.Error(ex, $"Failed to create published provider calculation results for specification: {specificationId}");
                throw new Exception($"Failed to create published provider calculation results for specification: {specificationId}", ex);
            }

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsAssemblerService.AssemblePublishedProviderResults(providerResults, author, specification);
            UpdateCacheForSegmentDone(specificationId, calculationProgress += 53, CalculationProgressStatus.InProgress);

            try
            {
                await _publishedProviderResultsRepository.SavePublishedResults(publishedProviderResults.ToList());
                UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);

                await SavePublishedAllocationLineResultVersionHistory(publishedProviderResults, specificationId);
                UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);

                await UpdateAllocationNotificationsFeedIndex(publishedProviderResults, specification);
                UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);
            }
            catch (Exception ex)
            {
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error);
                _logger.Error(ex, $"Failed to create published provider results for specification: {specificationId}");
                throw new Exception($"Failed to create published provider results for specification: {specificationId}", ex);
            }
            UpdateCacheForSegmentDone(specificationId, 100, CalculationProgressStatus.Finished);
        }

        void AssignRelatedCalculationResultIdsToAllocationResults(IEnumerable<PublishedProviderResult> publishedProviderResults, IEnumerable<PublishedProviderCalculationResult> publishedProviderCalcuationResults)
        {
            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                string fundingPeriodId = publishedProviderResult.FundingPeriod.Id;
                string providerId = publishedProviderResult.ProviderId;
                string specificationId = publishedProviderResult.SpecificationId;

                IEnumerable<PublishedProviderCalculationResult> calcResults = publishedProviderCalcuationResults.Where(m =>
                    m.FundingPeriod?.Id == fundingPeriodId &&
                    m.ProviderId == providerId &&
                    m.Specification?.Id == specificationId);
            }
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

            IEnumerable<string> providerResults = (await _resultsRepositoryPolicy.ExecuteAsync(() => _providerSourceDatasetRepository.GetAllScopedProviderIdsForSpecificationId(specificationId))).ToList();

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
                            if (specificationSummary == null)
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

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsForSpecificationId(specificationId));

            if (publishedProviderResults.IsNullOrEmpty())
            {
                return new OkObjectResult(Enumerable.Empty<PublishedProviderResultModel>());
            }

            IEnumerable<PublishedProviderResultModel> publishedProviderResultModels = MapPublishedProviderResultModels(publishedProviderResults);

            return new OkObjectResult(publishedProviderResultModels);
        }

        public async Task<IActionResult> GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(HttpRequest request)
        {

            var specificationId = GetParameter(request, "specificationId");
            var fundingPeriodId = GetParameter(request, "fundingPeriodId");
            var fundingStreamId = GetParameter(request, "fundingStreamId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No fundingPeriod Id was provided to GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingPeriod Id provided");
            }

            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No fundingStream Id was provided to GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingStream Id provided");
            }

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(fundingPeriodId, specificationId, fundingStreamId));

            if (publishedProviderResults.IsNullOrEmpty())
            {
                return new OkObjectResult(Enumerable.Empty<PublishedProviderResultModel>());
            }

            IEnumerable<PublishedProviderResultModel> publishedProviderResultModels = MapPublishedProviderResultModels(publishedProviderResults);

            return new OkObjectResult(publishedProviderResultModels);
        }


        public async Task<IActionResult> GetConfirmationDetailsForApprovePublishProviderResults(HttpRequest request)
        {
            var specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetConfirmationDetailsForApprovePublishProviderResults");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            string json = await request.GetRawBodyStringAsync();

            UpdatePublishedAllocationLineResultStatusModel filterCriteria = JsonConvert.DeserializeObject<UpdatePublishedAllocationLineResultStatusModel>(json);

            if (filterCriteria == null)
            {
                _logger.Error("Null filterCriteria was provided to GetConfirmationDetailsForApprovePublishProviderResults");

                return new BadRequestObjectResult("Null filterCriteria was provided");
            }

            if (filterCriteria.Providers.IsNullOrEmpty())
            {
                _logger.Error("Null or empty providers was provided to GetConfirmationDetailsForApprovePublishProviderResults");

                return new BadRequestObjectResult("Null or empty providers was provided");
            }

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsForSpecificationAndStatus(specificationId, filterCriteria));

            ConfirmPublishApproveModel confirmationDetails = new ConfirmPublishApproveModel
            {
                NumberOfProviders = publishedProviderResults.Select(r => r.FundingStreamResult.AllocationLineResult.Current.Provider.Id).Distinct().Count(),
                ProviderTypes = publishedProviderResults.Select(r => r.FundingStreamResult.AllocationLineResult.Current.Provider.ProviderType).Distinct().OrderBy(t => t).ToArray(),
                LocalAuthorities = publishedProviderResults.Select(r => r.FundingStreamResult.AllocationLineResult.Current.Provider.Authority).Distinct().OrderBy(a => a).ToArray(),
                FundingPeriod = publishedProviderResults.Select(r => r.FundingPeriod.Name).FirstOrDefault()
            };

            var fundingStreams = publishedProviderResults.Select(r => r.FundingStreamResult).GroupBy(r => r.FundingStream.Name);
            decimal totalFundingAmount = 0;
            foreach (IGrouping<string, PublishedFundingStreamResult> fundingStream in fundingStreams)
            {
                FundingStreamSummaryModel summary = new FundingStreamSummaryModel
                {
                    Name = fundingStream.Key,
                };

                foreach (PublishedFundingStreamResult result in fundingStream)
                {
                    AllocationLineSummaryModel existingAL = summary.AllocationLines.FirstOrDefault(a => a.Name == result.AllocationLineResult.AllocationLine.Name);

                    if (existingAL == null)
                    {
                        summary.AllocationLines.Add(new AllocationLineSummaryModel { Name = result.AllocationLineResult.AllocationLine.Name, Value = result.AllocationLineResult.Current.Value });
                    }
                    else
                    {
                        existingAL.Value += result.AllocationLineResult.Current.Value;
                    }

                    totalFundingAmount += result.AllocationLineResult.Current.Value ?? 0;
                }

                confirmationDetails.FundingStreams.Add(summary);
            }

            confirmationDetails.TotalFundingApproved = totalFundingAmount;
            return new OkObjectResult(confirmationDetails);
        }

        public async Task<IActionResult> UpdatePublishedAllocationLineResultsStatus(HttpRequest request)
        {
            var specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to UpdateAllocationLineResultStatus");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            string json = await request.GetRawBodyStringAsync();

            UpdatePublishedAllocationLineResultStatusModel updateStatusModel = JsonConvert.DeserializeObject<UpdatePublishedAllocationLineResultStatusModel>(json);

            if (updateStatusModel == null)
            {
                _logger.Error("Null updateStatusModel was provided to UpdateAllocationLineResultStatus");

                return new BadRequestObjectResult("Null updateStatusModel was provided");
            }

            if (updateStatusModel.Providers.IsNullOrEmpty())
            {
                _logger.Error("Null or empty providers was provided to UpdateAllocationLineResultStatus");

                return new BadRequestObjectResult("Null or empty providers was provided");
            }

            IEnumerable<string> providerIds = updateStatusModel.Providers.Select(p => p.ProviderId).Distinct();
            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsForSpecificationIdAndProviderId(specificationId, providerIds));

            if (publishedProviderResults.IsNullOrEmpty())
            {
                return new NotFoundObjectResult($"No provider results to update for specification id: {specificationId}");
            }

            try
            {
                Tuple<int, int> updateCounts = await UpdateAllocationLineResultsStatus(publishedProviderResults.ToList(), updateStatusModel, request, specificationId);

                return new OkObjectResult(new UpdateAllocationResultsStatusCounts { UpdatedAllocationLines = updateCounts.Item1, UpdatedProviderIds = updateCounts.Item2 });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update result status's");
                return new InternalServerErrorResult(ex.Message);
            }
        }

        public async Task<IActionResult> RemoveCurrentProviders()
        {
            try
            {
                await _searchRepository.DeleteIndex();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to delete providers index");

                return new InternalServerErrorResult(ex.Message);
            }

            bool cachedSummaryCountExists = await _cacheProvider.KeyExists<string>(CacheKeys.AllProviderSummaryCount);

            if (cachedSummaryCountExists)
            {
                await _cacheProvider.KeyDeleteAsync<string>(CacheKeys.AllProviderSummaryCount);
            }

            bool cachedSummariesExists = await _cacheProvider.KeyExists<List<ProviderSummary>>(CacheKeys.AllProviderSummaries);

            if (cachedSummariesExists)
            {
                await _cacheProvider.KeyDeleteAsync<List<ProviderSummary>>(CacheKeys.AllProviderSummaries);
            }

            return new NoContentResult();
        }

        public async Task<IActionResult> ImportProviders(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            MasterProviderModel[] providers = new MasterProviderModel[0];

            try
            {
                providers = JsonConvert.DeserializeObject<MasterProviderModel[]>(json);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Invalid providers were provided");

                return new BadRequestObjectResult("Invalid providers were provided");
            }

            if (providers.IsNullOrEmpty())
            {
                _logger.Error("No providers were provided");

                return new BadRequestObjectResult("No providers were provided");
            }

            IList<ProviderIndex> providersToIndex = new List<ProviderIndex>();

            foreach (MasterProviderModel provider in providers)
            {
                ProviderIndex providerIndex = _providerImportMappingService.Map(provider);

                if (providerIndex != null)
                {
                    providersToIndex.Add(providerIndex);
                }
            }

            IEnumerable<IndexError> errors = await _resultsSearchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Index(providersToIndex));

            if (errors.Any())
            {
                string errorMessage = $"Failed to index providers result documents with errors: { string.Join(";", errors.Select(m => m.ErrorMessage)) }";
                _logger.Error(errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }


            return new NoContentResult();
        }

        private static string GetParameter(HttpRequest request, string name)
        {
            if (request.Query.TryGetValue(name, out var parameter))
            {
                return parameter.FirstOrDefault();
            }
            return null;
        }

        public Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId, int maxResults = -1)
        {
            return _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetProviderResultsBySpecificationId(specificationId, maxResults));
        }

        public async Task<IActionResult> ReIndexAllocationNotificationFeeds()
        {
            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetAllNonHeldPublishedProviderResults());

            if (publishedProviderResults.IsNullOrEmpty())
            {
                _logger.Warning("No published provider results were found to index.");
            }
            else
            {
                IEnumerable<string> specificationIds = publishedProviderResults.DistinctBy(m => m.SpecificationId).Select(m => m.SpecificationId);

                foreach (string specificationId in specificationIds)
                {
                    SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));

                    try
                    {
                        await UpdateAllocationNotificationsFeedIndex(publishedProviderResults, specification);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failed to index allocation feeds");

                        return new InternalServerErrorResult(ex.Message);
                    }
                }
            }

            return new NoContentResult();
        }

        IEnumerable<PublishedProviderResultModel> MapPublishedProviderResultModels(IEnumerable<PublishedProviderResult> publishedProviderResults)
        {
            if (publishedProviderResults.IsNullOrEmpty())
            {
                return Enumerable.Empty<PublishedProviderResultModel>();
            }

            IList<PublishedProviderResultModel> results = new List<PublishedProviderResultModel>();

            IEnumerable<IGrouping<string, PublishedProviderResult>> providerResultsGroups = publishedProviderResults.GroupBy(m => m.ProviderId);

            foreach (IGrouping<string, PublishedProviderResult> providerResultGroup in providerResultsGroups)
            {
                IEnumerable<PublishedFundingStreamResult> fundingStreamResults = providerResultGroup.Select(m => m.FundingStreamResult);

                PublishedProviderResult providerResult = providerResultGroup.First();

                PublishedProviderResultModel publishedProviderResultModel = new PublishedProviderResultModel
                {
                    ProviderId = providerResult.ProviderId,
                    ProviderName = providerResult.FundingStreamResult.AllocationLineResult.Current.Provider?.Name
                };

                if (!results.Any(m => m.ProviderId == providerResultGroup.Key))
                {
                    results.Add(publishedProviderResultModel);
                }

                IEnumerable<IGrouping<string, PublishedFundingStreamResult>> fundingStreamResultsGroups = fundingStreamResults.GroupBy(m => m.FundingStream.Id);

                foreach (IGrouping<string, PublishedFundingStreamResult> fundingStreamResultsGroup in fundingStreamResultsGroups)
                {
                    IEnumerable<PublishedAllocationLineResult> allocationLineResults = fundingStreamResultsGroup.Select(m => m.AllocationLineResult);

                    PublishedFundingStreamResult fundingStreamResult = fundingStreamResultsGroup.First();

                    if (!publishedProviderResultModel.FundingStreamResults.Any(m => m.FundingStreamId == fundingStreamResultsGroup.Key))
                    {
                        publishedProviderResultModel.FundingStreamResults = publishedProviderResultModel.FundingStreamResults.Concat(new[] { new PublishedFundingStreamResultModel
                        {
                            FundingStreamId = fundingStreamResult.FundingStream.Id,
                            FundingStreamName = fundingStreamResult.FundingStream.Name,
                            AllocationLineResults = allocationLineResults.IsNullOrEmpty() ? Enumerable.Empty<PublishedAllocationLineResultModel>() : allocationLineResults.Select(
                                        alr => new PublishedAllocationLineResultModel
                                        {
                                            AllocationLineId = alr.AllocationLine.Id,
                                            AllocationLineName = alr.AllocationLine.Name,
                                            FundingAmount = alr.Current.Value,
                                            Status = alr.Current.Status,
                                            LastUpdated = alr.Current.Date,
                                            Authority = alr.Current.Provider.Authority
                                        }
                                    )
                        } });
                    }

                }
            }

            return results;
        }

        private async Task<Tuple<int, int>> UpdateAllocationLineResultsStatus(IEnumerable<PublishedProviderResult> publishedProviderResults,
            UpdatePublishedAllocationLineResultStatusModel updateStatusModel, HttpRequest request, string specificationId)
        {
            IList<string> updatedAllocationLineIds = new List<string>();
            IList<string> updatedProviderIds = new List<string>();
            IList<PublishedProviderResult> resultsToProfile = new List<PublishedProviderResult>();

            List<PublishedProviderResult> resultsToUpdate = new List<PublishedProviderResult>();

            IEnumerable<PublishedAllocationLineResultHistory> historyResultsToUpdate = new List<PublishedAllocationLineResultHistory>();

            Reference author = request.GetUser();

            foreach (UpdatePublishedAllocationLineResultStatusProviderModel providerstatusModel in updateStatusModel.Providers)
            {
                if (providerstatusModel.AllocationLineIds.IsNullOrEmpty())
                {
                    continue;
                }

                IEnumerable<PublishedProviderResult> results = publishedProviderResults.Where(m => m.ProviderId == providerstatusModel.ProviderId);

                if (results.IsNullOrEmpty())
                {
                    continue;
                }

                IEnumerable<PublishedAllocationLineResult> publishedAllocationLineResults = results.Select(m => m.FundingStreamResult.AllocationLineResult).ToArraySafe();

                if (publishedAllocationLineResults.IsNullOrEmpty())
                {
                    continue;
                }

                bool isUpdated = false;

                foreach (string allocationLineResultId in providerstatusModel.AllocationLineIds)
                {
                    IEnumerable<PublishedAllocationLineResult> allocationLineResults = publishedAllocationLineResults.Where(m => m.AllocationLine.Id == allocationLineResultId);

                    foreach (PublishedAllocationLineResult allocationLineResult in allocationLineResults)
                    {

                        if (CanUpdateAllocationLineResult(allocationLineResult, updateStatusModel.Status))
                        {
                            PublishedAllocationLineResultHistory historyResult = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderAllocationLineHistoryForSpecificationIdAndProviderId(specificationId, providerstatusModel.ProviderId, allocationLineResult.AllocationLine.Id));

                            int nextVersionIndex = historyResult.History.IsNullOrEmpty() ? 1 : historyResult.History.Max(m => m.Version) + 1;

                            PublishedAllocationLineResultVersion newVersion = CreateNewPublishedAllocationLineResultVersion(allocationLineResult, author, updateStatusModel.Status, nextVersionIndex);

                            if (historyResult != null)
                            {
                                if (historyResult.History == null)
                                {
                                    historyResult.History = Enumerable.Empty<PublishedAllocationLineResultVersion>();
                                }

                                historyResult.History = (historyResult.History.Concat(new[] { newVersion.Clone() })).ToList();

                                historyResultsToUpdate = historyResultsToUpdate.Concat(new[] { historyResult });
                            }

                            if (!updatedAllocationLineIds.Contains(allocationLineResultId))
                            {
                                updatedAllocationLineIds.Add(allocationLineResultId);
                            }

                            if (!updatedProviderIds.Contains(providerstatusModel.ProviderId))
                            {
                                updatedProviderIds.Add(providerstatusModel.ProviderId);
                            }

                            isUpdated = true;
                        }
                    }
                }


                if (isUpdated)
                {
                    foreach (PublishedProviderResult result in results)
                    {
                        result.Title = $"Allocation {result.FundingStreamResult.AllocationLineResult.AllocationLine.Name} was {result.FundingStreamResult.AllocationLineResult.Current.Status.ToString()}";

                        if(updateStatusModel.Status == AllocationLineStatus.Approved)
                        {
                            resultsToProfile.Add(result);
                        }
                    }

                    resultsToUpdate.AddRange(results);
                }
            }

            if (resultsToUpdate.Any())
            {
                SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));

                try
                {
                    await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.SavePublishedResults(resultsToUpdate));

                    await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.SavePublishedAllocationLineResultsHistory(historyResultsToUpdate.ToList()));

                    await UpdateAllocationNotificationsFeedIndex(resultsToUpdate, specification);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed when updating allocation line results");

                    throw new Exception("Failed when updating allocation line results");
                }
            }

            await GetProfilingPeriods(request, resultsToProfile, specificationId);

            return new Tuple<int, int>(updatedAllocationLineIds.Count, updatedProviderIds.Count);
        }

        private async Task GetProfilingPeriods(HttpRequest request, IEnumerable<PublishedProviderResult> resultsToProfile, string specificationId)
        {
            int batchSize = 100;
            int startPosition = 0;
            while (startPosition < resultsToProfile.Count())
            {
                IEnumerable<FetchProviderProfilingMessageItem> batchOfResultsToProfile = resultsToProfile.Skip(startPosition).Take(batchSize).Select(r => new FetchProviderProfilingMessageItem { AllocationLineResultId = r.Id, ProviderId = r.ProviderId });

                _logger.Information($"Sending new provider profiling message for {batchOfResultsToProfile.Count()} results");

                IDictionary<string, string> properties = request.BuildMessageProperties();
                properties["specification-id"] = specificationId;

                await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.FetchProviderProfile, batchOfResultsToProfile, properties);

                _logger.Information($"Sent new provider profiling message for {batchOfResultsToProfile.Count()} results");

                startPosition += batchOfResultsToProfile.Count();
            }
        }

        public async Task FetchProviderProfile(Message message)
        {
            Stopwatch profilingStopWatch = Stopwatch.StartNew();
        
            Guard.ArgumentNotNull(message, nameof(message));

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("No specification id was present on the message");
                throw new ArgumentException("Message must contain a specification id in user properties");
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            IEnumerable<FetchProviderProfilingMessageItem> data = message.GetPayloadAsInstanceOf<IEnumerable<FetchProviderProfilingMessageItem>>();

            if (data.IsNullOrEmpty())
            {
                _logger.Error("No allocation result profiling items were present in the message");
                throw new ArgumentException("Message must contain a collection of allocation results profiling items");
            }
            
            SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));

            if(specification == null)
            {
                _logger.Error($"A specification could not be found with id {specificationId}");

                throw new ArgumentException($"Could not find a specification with id {specificationId}");
            }

            ConcurrentBag<PublishedProviderResult> publishedProviderResults = new ConcurrentBag<PublishedProviderResult>();

            IList<Task> profilingTasks = new List<Task>();

            long totalMsForProfilingApi = 0;

            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (FetchProviderProfilingMessageItem profilingItem in data)
            {
                await throttler.WaitAsync();
                profilingTasks.Add(
                    Task.Run(() =>
                    {
                        try
                        {
                            (PublishedProviderResult publishedProviderResult, long timeInMs) profilingResult = ProfileResult(profilingItem).Result;

                            publishedProviderResults.Add(profilingResult.publishedProviderResult);

                            totalMsForProfilingApi += profilingResult.timeInMs;
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            
            await TaskHelper.WhenAllAndThrow(profilingTasks.ToArray());

            if (!publishedProviderResults.IsNullOrEmpty())
            {
                await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.SavePublishedResults(publishedProviderResults));

                await UpdateAllocationNotificationsFeedIndex(publishedProviderResults, specification, true);
            }

            profilingStopWatch.Stop();

            int batchSize = data.Count();

            IDictionary<string, double> metrics = new Dictionary<string, double>()
                    {
                        { "profiling-batch-size-count", batchSize },
                        { "profiling-run-timeInMs", profilingStopWatch.ElapsedMilliseconds },
                        { "average-time-for-profiling-api", (long)(totalMsForProfilingApi / batchSize) }
                    };

            _telemetry.TrackEvent("ProviderProfilingProcessed",
                    new Dictionary<string, string>()
                    {
                        { "specificationId" , specificationId }                      
                    },
                    metrics
                );
        }

        private async Task<(PublishedProviderResult, long)> ProfileResult(FetchProviderProfilingMessageItem messageItem)
        {
            PublishedProviderResult result = await _publishedProviderResultsRepository.GetPublishedProviderResultForId(messageItem.AllocationLineResultId, messageItem.ProviderId);

            if (result == null)
            {
                _logger.Error("Could not find published provider result with id '{id}'", messageItem.AllocationLineResultId);
                throw new ArgumentException($"Published provider result with id '{messageItem.AllocationLineResultId}' not found");
            }

            ProviderProfilingRequestModel providerProfilingRequestModel = new ProviderProfilingRequestModel
            {
                FundingStreamPeriod = result.FundingStreamResult.FundingStreamPeriod,
                AllocationValueByDistributionPeriod = new[]
                {
                        new AllocationPeriodValue
                        {
                            DistributionPeriod =result.FundingStreamResult.DistributionPeriod,
                            AllocationValue = (decimal)result.FundingStreamResult.AllocationLineResult.Current.Value
                        }
                    }
            };

            Stopwatch profilingApiStopWatch = Stopwatch.StartNew();

            ProviderProfilingResponseModel responseModel = await _providerProfilingRepositoryPolicy.ExecuteAsync(() => _providerProfilingRepository.GetProviderProfilePeriods(providerProfilingRequestModel));

            profilingApiStopWatch.Stop();

            if (responseModel != null && !responseModel.DeliveryProfilePeriods.IsNullOrEmpty())
            {
                result.ProfilingPeriods = responseModel.DeliveryProfilePeriods.ToArraySafe();

                return (result, profilingApiStopWatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.Error($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}");

                throw new Exception($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}");
            }
        }

        bool CanUpdateAllocationLineResult(PublishedAllocationLineResult allocationLineResult, AllocationLineStatus newStatus)
        {
            if (allocationLineResult == null || allocationLineResult.Current.Status == newStatus
                || (allocationLineResult.Current.Status == AllocationLineStatus.Held && newStatus == AllocationLineStatus.Published)
                || (allocationLineResult.Current.Status == AllocationLineStatus.Approved && newStatus == AllocationLineStatus.Held)
                || (allocationLineResult.Current.Status == AllocationLineStatus.Published && newStatus == AllocationLineStatus.Held)
                || (allocationLineResult.Current.Status == AllocationLineStatus.Published && newStatus == AllocationLineStatus.Approved))
            {
                return false;
            }

            return true;
        }

        PublishedAllocationLineResultVersion CreateNewPublishedAllocationLineResultVersion(PublishedAllocationLineResult allocationLineResult,
            Reference author, AllocationLineStatus newStatus, int nextVersionIndex)
        {
            PublishedAllocationLineResultVersion newVersion = allocationLineResult.Current.Clone();
            newVersion.Date = DateTimeOffset.Now;
            newVersion.Author = author;
            newVersion.Status = newStatus;
            newVersion.Version = nextVersionIndex;

            allocationLineResult.Current = newVersion;

            if (newStatus == AllocationLineStatus.Published)
            {
                allocationLineResult.Published = newVersion;
            }

            return newVersion;
        }

        async Task SavePublishedAllocationLineResultVersionHistory(IEnumerable<PublishedProviderResult> publishedProviderResults, string specificationId)
        {
            IEnumerable<PublishedAllocationLineResultHistory> historyResults = (await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderAllocationLineHistoryForSpecificationId(specificationId)))?.ToList();

            IEnumerable<PublishedAllocationLineResultHistory> historyResultsToSave = new List<PublishedAllocationLineResultHistory>();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                PublishedAllocationLineResult publishedAllocationLineResult = publishedProviderResult.FundingStreamResult.AllocationLineResult;

                IEnumerable<PublishedAllocationLineResultHistory> publishedAllocationLineResultHistoryList = historyResults?.Where(m => m.ProviderId == publishedProviderResult.ProviderId);

                PublishedAllocationLineResultHistory publishedAllocationLineResultHistory = publishedAllocationLineResultHistoryList?.FirstOrDefault(m => m.AllocationResultId == publishedProviderResult.Id);

                if (publishedAllocationLineResultHistory == null)
                {
                    publishedAllocationLineResultHistory = new PublishedAllocationLineResultHistory
                    {
                        ProviderId = publishedProviderResult.ProviderId,
                        AllocationResultId = publishedProviderResult.Id,
                        SpecificationId = specificationId,
                        AllocationLine = publishedAllocationLineResult.AllocationLine,
                        History = new[] { publishedAllocationLineResult.Current }
                    };
                }
                else
                {
                    publishedAllocationLineResultHistory.History = publishedAllocationLineResultHistory.History.Concat(new[] { publishedAllocationLineResult.Current });
                }

                historyResultsToSave = historyResultsToSave.Concat(new[] { publishedAllocationLineResultHistory });

            }

            await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.SavePublishedAllocationLineResultsHistory(historyResultsToSave));
        }

        async Task SavePublishedCalculationResultVersionHistory(IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults, string specificationId)
        {
            IEnumerable<PublishedProviderCalculationResultHistory> historyResults = (await _publishedProviderCalculationResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderCalculationResultsRepository.GetPublishedProviderCalculationHistoryForSpecificationId(specificationId)))?.ToList();

            IEnumerable<PublishedProviderCalculationResultHistory> historyResultsToSave = new List<PublishedProviderCalculationResultHistory>();

            foreach (PublishedProviderCalculationResult publishedProviderCalculationResult in publishedProviderCalculationResults)
            {

                IEnumerable<PublishedProviderCalculationResultHistory> publishedCalculationResultHistoryList = historyResults?.Where(m => m.ProviderId == publishedProviderCalculationResult.ProviderId);

                PublishedProviderCalculationResultHistory publishedCalculationResultHistory = publishedCalculationResultHistoryList?.FirstOrDefault(m => m.CalculationnResultId == publishedProviderCalculationResult.Id);

                if (publishedCalculationResultHistory == null)
                {
                    if (publishedProviderCalculationResult.Current != null)
                    {
                        publishedCalculationResultHistory = new PublishedProviderCalculationResultHistory
                        {
                            ProviderId = publishedProviderCalculationResult.Current.Provider.Id,
                            CalculationnResultId = publishedProviderCalculationResult.Id,
                            SpecificationId = specificationId,
                            History = new[] { publishedProviderCalculationResult.Current }
                        };
                    }
                    else
                    {
                        _logger.Error($"Null current object found on published calculation result for id: {publishedProviderCalculationResult.Id}");
                        continue;
                    }
                }
                else
                {
                    publishedCalculationResultHistory.History = publishedCalculationResultHistory.History.Concat(new[] { publishedProviderCalculationResult.Current });
                }

                if (publishedCalculationResultHistory != null)
                {
                    historyResultsToSave = historyResultsToSave.Concat(new[] { publishedCalculationResultHistory });
                }

            }

            if (!historyResultsToSave.IsNullOrEmpty())
            {
                await _publishedProviderCalculationResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderCalculationResultsRepository.SavePublishedCalculationResultsHistory(historyResultsToSave));
            }
        }

        async Task
            UpdateAllocationNotificationsFeedIndex(IEnumerable<PublishedProviderResult> publishedProviderResults, SpecificationCurrentVersion specification, bool checkProfiling = false)
        {
            IEnumerable<AllocationNotificationFeedIndex> notifications = await BuildAllocationNotificationIndexItems(publishedProviderResults, specification, checkProfiling);

            if (notifications.Any())
            {
                IEnumerable<IndexError> errors = await _allocationNotificationsSearchRepositoryPolicy.ExecuteAsync(() => _allocationNotificationsSearchRepository.Index(notifications));

                if (errors.Any())
                {
                    string errorMessage = $"Failed to index allocation notification feed documents with errors: { string.Join(";", errors.Select(m => m.ErrorMessage)) }";
                    _logger.Error(errorMessage);

                    throw new Exception(errorMessage);
                }
            }
        }

        async Task<IEnumerable<AllocationNotificationFeedIndex>> BuildAllocationNotificationIndexItems(IEnumerable<PublishedProviderResult> publishedProviderResults, SpecificationCurrentVersion specification, bool checkProfiling = false)
        {
            Guard.ArgumentNotNull(publishedProviderResults, nameof(publishedProviderResults));

            Guard.ArgumentNotNull(specification, nameof(specification));

            IList<AllocationNotificationFeedIndex> notifications = new List<AllocationNotificationFeedIndex>();

            Stopwatch fetchCalcResultsStopwatch = new Stopwatch();
            fetchCalcResultsStopwatch.Start();
            IEnumerable<string> providerIds = publishedProviderResults.Select(r => r.ProviderId).Distinct();
            IEnumerable<PublishedProviderCalculationResult> calculationResults = (await _publishedProviderCalculationResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderCalculationResultsRepository.GetPublishedProviderCalculationResultsBySpecificationIdAndProviderId(specification.Id, providerIds))).ToList();
            fetchCalcResultsStopwatch.Stop();
            _telemetry.TrackEvent("Fetching Published Calculation Results",
                new Dictionary<string, string>
                {
                    { "specificationId", specification.Id }
                },
                new Dictionary<string, double>
                {
                    { "provider-batch-size", providerIds.Count() },
                    { "calculation-results-found", calculationResults.Count() },
                    { "duration", fetchCalcResultsStopwatch.ElapsedMilliseconds }
                });

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                if (publishedProviderResult.FundingStreamResult == null || publishedProviderResult.FundingStreamResult.AllocationLineResult == null || publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Held)
                {
                    continue;
                }

                string providerProfiles = "[]";

                if (checkProfiling && publishedProviderResult.ProfilingPeriods.IsNullOrEmpty())
                {
                    string message = $"Provider result with id {publishedProviderResult.Id} and provider id {publishedProviderResult.ProviderId} contains no profiling periods";

                    _logger.Error(message);

                    throw new MissingProviderProfilesException(publishedProviderResult.Id, publishedProviderResult.ProviderId);
                }
                else
                {
                    providerProfiles = JsonConvert.SerializeObject(publishedProviderResult.ProfilingPeriods);
                }

                IEnumerable<PublishedProviderCalculationResult> providerCalculationResults = calculationResults.Where(m => m.ProviderId == publishedProviderResult.ProviderId);

                notifications.Add(new AllocationNotificationFeedIndex
                {
                    Id = publishedProviderResult.Id,
                    Title = publishedProviderResult.Title,
                    Summary = publishedProviderResult.Summary,
                    DatePublished = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Published
                        ? publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Date : (DateTimeOffset?)null,
                    DateUpdated = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Date,
                    FundingStreamId = publishedProviderResult.FundingStreamResult.FundingStream.Id,
                    FundingStreamName = publishedProviderResult.FundingStreamResult.FundingStream.Name,
                    FundingStreamShortName = publishedProviderResult.FundingStreamResult.FundingStream.ShortName,
                    FundingStreamPeriodId = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType?.Id,
                    FundingStreamStartDay = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.StartDay,
                    FundingStreamStartMonth = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.StartMonth,
                    FundingStreamEndDay = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.EndDay,
                    FundingStreamEndMonth = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.EndMonth,
                    FundingStreamPeriodName = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType?.Name,
                    FundingPeriodId = publishedProviderResult.FundingPeriod.Id,
                    FundingPeriodStartYear = publishedProviderResult.FundingPeriod.StartYear,
                    FundingPeriodEndYear = publishedProviderResult.FundingPeriod.EndYear,
                    ProviderId = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Id,
                    ProviderUkPrn = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.UKPRN,
                    ProviderUpin = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.UPIN,
                    ProviderUrn = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.URN,
                    ProviderOpenDate = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.DateOpened,
                    ProviderClosedDate = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.DateOpened,
                    AllocationLineId = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id,
                    AllocationLineName = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Name,
                    AllocationVersionNumber = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Version,
                    AllocationStatus = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status.ToString(),
                    AllocationAmount = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Value.HasValue
                                            ? Convert.ToDouble(publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Value) : 0,
                    AllocationLineContractRequired = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.IsContractRequired,
                    AllocationLineFundingRoute = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.FundingRoute.ToString(),
                    AllocationLineShortName = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.ShortName,
                    ProviderProfiling = providerProfiles,
                    ProviderName = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Name,
                    LaCode = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.LACode,
                    Authority = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Authority,
                    ProviderType = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.ProviderType,
                    SubProviderType = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.ProviderSubType,
                    EstablishmentNumber = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.EstablishmentNumber,
                    CrmAccountId = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.CrmAccountId,
                    NavVendorNo = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.NavVendorNo,
                    DfeEstablishmentNumber = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.DfeEstablishmentNumber,
                    ProviderStatus = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Status,
                    PolicySummaries = JsonConvert.SerializeObject(CreatePolicySummaries(providerCalculationResults, specification))
                });
            }

            return notifications;
        }

        IEnumerable<PublishedProviderResultsPolicySummary> CreatePolicySummaries(IEnumerable<PublishedProviderCalculationResult> calculationResults, SpecificationCurrentVersion specification)
        {
            IList<PublishedProviderResultsPolicySummary> policySummaries = new List<PublishedProviderResultsPolicySummary>();

            foreach (Policy policy in specification.Policies)
            {
                PublishedProviderResultsPolicySummary publishedProviderResultsPolicySummary = new PublishedProviderResultsPolicySummary
                {
                    Policy = new PolicySummary(policy.Id, policy.Name, policy.Description),
                    Calculations = AddCalculationSummaries(policy, calculationResults).ToArraySafe(),
                };

                foreach (Policy subPolicy in policy.SubPolicies)
                {
                    PublishedProviderResultsPolicySummary publishedProviderResultsSubPolicySummary = new PublishedProviderResultsPolicySummary
                    {
                        Policy = new PolicySummary(subPolicy.Id, subPolicy.Name, subPolicy.Description),
                        Calculations = AddCalculationSummaries(subPolicy, calculationResults).ToArraySafe(),
                    };

                    publishedProviderResultsPolicySummary.Policies = publishedProviderResultsPolicySummary.Policies.Concat(new[] { publishedProviderResultsSubPolicySummary }).ToArraySafe();
                }

                policySummaries.Add(publishedProviderResultsPolicySummary);

            }

            return policySummaries;
        }

        IEnumerable<PublishedProviderResultsCalculationSummary> AddCalculationSummaries(Policy policy, IEnumerable<PublishedProviderCalculationResult> calculationResults)
        {
            IEnumerable<PublishedProviderResultsCalculationSummary> calculationSummaries = Enumerable.Empty<PublishedProviderResultsCalculationSummary>();

            if (policy.Calculations.IsNullOrEmpty())
            {
                return calculationSummaries;
            }

            foreach (Calculation calculation in policy.Calculations)
            {
                if (calculation.CalculationType == CalculationType.Number && calculation.IsPublic == false)
                {
                    continue;
                }

                PublishedProviderCalculationResult publishedProviderCalculationResult = calculationResults.FirstOrDefault(m => m.CalculationSpecification.Id == calculation.Id);

                if (publishedProviderCalculationResult == null)
                {
                    _logger.Error($"Failed to find published calculation result for calculation id {calculation.Id}");

                    continue;
                }

                PublishedProviderResultsCalculationSummary calculationSummary = new PublishedProviderResultsCalculationSummary
                {
                    Amount = publishedProviderCalculationResult.Current.Value.HasValue ? publishedProviderCalculationResult.Current.Value.Value : 0,
                    CalculationType = publishedProviderCalculationResult.Current.CalculationType,
                    Name = publishedProviderCalculationResult.CalculationSpecification.Name,
                    Version = publishedProviderCalculationResult.Current.Version
                };

                calculationSummaries = calculationSummaries.Concat(new[] { calculationSummary });
            }

            return calculationSummaries;
        }

        private void UpdateCacheForSegmentDone(string specificationId, int percentageToSetTo, CalculationProgressStatus progressStatus, string message = null)
        {
            SpecificationCalculationExecutionStatus calculationProgress = new SpecificationCalculationExecutionStatus(specificationId, percentageToSetTo, progressStatus)
            {
                ErrorMessage = message
            };
            CacheHelper.UpdateCacheForItem($"{CacheKeys.CalculationProgress}{calculationProgress.SpecificationId}", calculationProgress, _cacheProvider);
        }
    }
}