using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;

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
        private readonly IProviderImportMappingService _providerImportMappingService;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMessengerService _messengerService;
        private readonly ICalculationsRepository _calculationRepository;
        private readonly Polly.Policy _calculationsRepositoryPolicy;

        public ResultsService(ILogger logger,
            ICalculationResultsRepository resultsRepository,
            IMapper mapper,
            ISearchRepository<ProviderIndex> searchRepository,
            ITelemetry telemetry,
            IProviderSourceDatasetRepository providerSourceDatasetRepository,
            ISearchRepository<CalculationProviderResultsIndex> calculationProviderResultsSearchRepository,
            ISpecificationsRepository specificationsRepository,
            IResultsResilliencePolicies resiliencePolicies,
            IProviderImportMappingService providerImportMappingService,
            ICacheProvider cacheProvider,
            IMessengerService messengerService,
            ICalculationsRepository calculationRepository)
        {
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(providerSourceDatasetRepository, nameof(providerSourceDatasetRepository));
            Guard.ArgumentNotNull(calculationProviderResultsSearchRepository, nameof(calculationProviderResultsSearchRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(providerImportMappingService, nameof(providerImportMappingService));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(calculationRepository, nameof(calculationRepository));

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
            _providerImportMappingService = providerImportMappingService;
            _cacheProvider = cacheProvider;
            _messengerService = messengerService;
            _calculationRepository = calculationRepository;
            _calculationsRepositoryPolicy = resiliencePolicies.CalculationsRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker)_resultsRepository).IsHealthOk();
            (bool Ok, string Message) searchRepoHealth = await _searchRepository.IsHealthOk();
            ServiceHealth providerSourceDatasetRepoHealth = await ((IHealthChecker)_providerSourceDatasetRepository).IsHealthOk();
            (bool Ok, string Message) calcSearchRepoHealth = await _calculationProviderResultsSearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ResultsService)
            };
            health.Dependencies.AddRange(datasetsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });
            health.Dependencies.AddRange(providerSourceDatasetRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = calcSearchRepoHealth.Ok, DependencyName = _calculationProviderResultsSearchRepository.GetType().GetFriendlyName(), Message = calcSearchRepoHealth.Message });

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
            string providerId = GetParameter(request, "providerId");

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
            string providerId = GetParameter(request, "providerId");
            string specificationId = GetParameter(request, "specificationId");

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
            string specificationId = GetParameter(request, "specificationId");

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
            string providerId = GetParameter(request, "providerId");
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

        public async Task<IActionResult> GetProviderSourceDatasetsByProviderIdAndSpecificationId(HttpRequest request)
        {
            string specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetProviderResultsBySpecificationId");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            string providerId = GetParameter(request, "providerId");

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
            string specificationId = GetParameter(request, "specificationId");

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
                        CalculationResult = calculationResult.Value.HasValue ? Convert.ToDouble(calculationResult.Value) : default(double?),
                        IsExcluded = !calculationResult.Value.HasValue
                    });
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

        public async Task<IActionResult> HasCalculationResults(string calculationId)
        {
            Guard.IsNullOrWhiteSpace(calculationId, nameof(calculationId));

            Models.Calcs.Calculation calculation = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationRepository.GetCalculationById(calculationId));
            
            if(calculation == null)
            {
                _logger.Error($"Calculation could not be found for calculation id '{calculationId}'");

                return new NotFoundObjectResult($"Calculation could not be found for calculation id '{calculationId}'");
            }
            bool hasCalculationsResults = false;

            ProviderResult providerResult = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetSingleProviderResultBySpecificationId(calculation.SpecificationId));

            if(providerResult != null)
            {
                CalculationResult calculationResult = providerResult.CalculationResults?.FirstOrDefault(m => string.Equals(m.Calculation.Id, calculationId, StringComparison.InvariantCultureIgnoreCase));

                if(calculationResult != null)
                {
                    hasCalculationsResults = true;
                }
            }

            return new OkObjectResult(hasCalculationsResults);
        }

        private static string GetParameter(HttpRequest request, string name)
        {
            if (request.Query.TryGetValue(name, out Microsoft.Extensions.Primitives.StringValues parameter))
            {
                return parameter.FirstOrDefault();
            }
            return null;
        }

        public Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId, int maxResults = -1)
        {
            return _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetProviderResultsBySpecificationId(specificationId, maxResults));
        }
    }
}