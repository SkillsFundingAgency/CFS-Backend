using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
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
        private readonly IProviderSourceDatasetRepository _providerSourceDatasetRepository;
        private readonly ISearchRepository<ProviderCalculationResultsIndex> _calculationProviderResultsSearchRepository;
        private readonly Polly.Policy _resultsRepositoryPolicy;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly Polly.Policy _resultsSearchRepositoryPolicy;
        private readonly Polly.Policy _specificationsRepositoryPolicy;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMessengerService _messengerService;
        private readonly ICalculationsRepository _calculationRepository;
        private readonly Polly.Policy _calculationsRepositoryPolicy;
        private readonly IValidator<MasterProviderModel> _masterProviderModelValidator;
        private readonly IFeatureToggle _featureToggle;

        public ResultsService(ILogger logger,
            IFeatureToggle featureToggle,
            ICalculationResultsRepository resultsRepository,
            IMapper mapper,
            ITelemetry telemetry,
            IProviderSourceDatasetRepository providerSourceDatasetRepository,
            ISearchRepository<ProviderCalculationResultsIndex> calculationProviderResultsSearchRepository,
            ISpecificationsRepository specificationsRepository,
            IResultsResiliencePolicies resiliencePolicies,
            ICacheProvider cacheProvider,
            IMessengerService messengerService,
            ICalculationsRepository calculationRepository,
            IValidator<MasterProviderModel> masterProviderModelValidator)
        {
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(providerSourceDatasetRepository, nameof(providerSourceDatasetRepository));
            Guard.ArgumentNotNull(calculationProviderResultsSearchRepository, nameof(calculationProviderResultsSearchRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(calculationRepository, nameof(calculationRepository));
            Guard.ArgumentNotNull(masterProviderModelValidator, nameof(masterProviderModelValidator));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _logger = logger;
            _resultsRepository = resultsRepository;
            _mapper = mapper;
            _telemetry = telemetry;
            _providerSourceDatasetRepository = providerSourceDatasetRepository;
            _calculationProviderResultsSearchRepository = calculationProviderResultsSearchRepository;
            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _specificationsRepository = specificationsRepository;
            _resultsSearchRepositoryPolicy = resiliencePolicies.ResultsSearchRepository;
            _specificationsRepositoryPolicy = resiliencePolicies.SpecificationsRepository;
            _cacheProvider = cacheProvider;
            _messengerService = messengerService;
            _calculationRepository = calculationRepository;
            _masterProviderModelValidator = masterProviderModelValidator;
            _calculationsRepositoryPolicy = resiliencePolicies.CalculationsRepository;
            _featureToggle = featureToggle;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth datasetsRepoHealth = await ((IHealthChecker)_resultsRepository).IsHealthOk();
            ServiceHealth providerSourceDatasetRepoHealth = await ((IHealthChecker)_providerSourceDatasetRepository).IsHealthOk();
            (bool Ok, string Message) calcSearchRepoHealth = await _calculationProviderResultsSearchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ResultsService)
            };
            health.Dependencies.AddRange(datasetsRepoHealth.Dependencies);
            health.Dependencies.AddRange(providerSourceDatasetRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = calcSearchRepoHealth.Ok, DependencyName = _calculationProviderResultsSearchRepository.GetType().GetFriendlyName(), Message = calcSearchRepoHealth.Message });

            return health;
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

            IList<ProviderCalculationResultsIndex> searchItems = new List<ProviderCalculationResultsIndex>();

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

                    ProviderCalculationResultsIndex searchItem = new ProviderCalculationResultsIndex
                    {
                        SpecificationId = providerResult.SpecificationId,
                        SpecificationName = specificationSummary?.Name,
                        CalculationName = providerResult.CalculationResults.Select(x => x.Calculation.Name).ToArraySafe(),
                        CalculationId = providerResult.CalculationResults.Select(x => x.Calculation.Id).ToArraySafe(),
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
                        CalculationResult = providerResult.CalculationResults.Select(m => m.Value.HasValue ? m.Value.ToString() : "null").ToArraySafe()
                    };

                    if (_featureToggle.IsExceptionMessagesEnabled())
                    {
                        searchItem.CalculationException = providerResult.CalculationResults
                            .Where(m => !string.IsNullOrWhiteSpace(m.ExceptionType))
                            .Select(e => e.Calculation.Id)
                            .ToArraySafe();

                        searchItem.CalculationExceptionType = providerResult.CalculationResults
                            .Select(m => m.ExceptionType ?? string.Empty)
                            .ToArraySafe();

                        searchItem.CalculationExceptionMessage = providerResult.CalculationResults
                            .Select(m => m.ExceptionMessage ?? string.Empty)
                            .ToArraySafe();
                    }

                    searchItems.Add(searchItem);
                }
            }

            for (int i = 0; i < searchItems.Count; i += 500)
            {
                IEnumerable<ProviderCalculationResultsIndex> partitionedResults = searchItems.Skip(i).Take(500);

                IEnumerable<IndexError> errors = await _resultsSearchRepositoryPolicy.ExecuteAsync(() => _calculationProviderResultsSearchRepository.Index(partitionedResults));

                if (errors.Any())
                {
                    _logger.Error($"Failed to index calculation provider result documents with errors: { string.Join(";", errors.Select(m => m.ErrorMessage)) }");

                    return new StatusCodeResult(500);
                }
            }

            return new NoContentResult();
        }

        public async Task CleanupProviderResultsForSpecification(Message message)
        {
            string specificationId = message.UserProperties["specificationId"].ToString();

            Models.Results.SpecificationProviders specificationProviders = message.GetPayloadAsInstanceOf<Models.Results.SpecificationProviders>();

            IEnumerable<ProviderResult> providerResults = await _resultsRepositoryPolicy
                .ExecuteAsync(() => _resultsRepository.GetProviderResultsBySpecificationIdAndProviders(specificationProviders.Providers, specificationId)
            );

            if (providerResults.Any())
            {
                _logger.Information($"Removing {specificationProviders.Providers.Count()} from calculation results for specification {specificationId}");

                await _resultsRepositoryPolicy
                    .ExecuteAsync(() => _resultsRepository.DeleteCurrentProviderResults(providerResults)
                );

                SearchResults<ProviderCalculationResultsIndex> indexItems = await _resultsSearchRepositoryPolicy
                    .ExecuteAsync(() => _calculationProviderResultsSearchRepository.Search(string.Empty,
                            new SearchParameters
                            {
                                Top = providerResults.Count(),
                                SearchMode = SearchMode.Any,
                                Filter = $"specificationId eq '{specificationId}' and (" + string.Join(" or ", providerResults.Select(m => $"providerId eq '{m.Provider.Id}'")) + ")",
                                QueryType = QueryType.Full
                            }
                        )
                    );

                await _resultsSearchRepositoryPolicy.ExecuteAsync(() => _calculationProviderResultsSearchRepository.Remove(indexItems?.Results.Select(m => m.Result)));
            }
        }

        public async Task<IActionResult> HasCalculationResults(string calculationId)
        {
            Guard.IsNullOrWhiteSpace(calculationId, nameof(calculationId));

            Models.Calcs.Calculation calculation = await _calculationsRepositoryPolicy.ExecuteAsync(() => _calculationRepository.GetCalculationById(calculationId));

            if (calculation == null)
            {
                _logger.Error($"Calculation could not be found for calculation id '{calculationId}'");

                return new NotFoundObjectResult($"Calculation could not be found for calculation id '{calculationId}'");
            }
            bool hasCalculationsResults = false;

            ProviderResult providerResult = await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetSingleProviderResultBySpecificationId(calculation.SpecificationId));

            if (providerResult != null)
            {
                CalculationResult calculationResult = providerResult.CalculationResults?.FirstOrDefault(m => string.Equals(m.Calculation.Id, calculationId, StringComparison.InvariantCultureIgnoreCase));

                if (calculationResult != null)
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