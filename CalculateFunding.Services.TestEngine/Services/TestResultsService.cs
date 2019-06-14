using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class TestResultsService : ITestResultsService, IHealthChecker
    {
        private readonly ITestResultsRepository _testResultsRepository;
        private readonly ISearchRepository<TestScenarioResultIndex> _searchRepository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly Polly.Policy _testResultsPolicy;
        private readonly Polly.Policy _testResultsSearchPolicy;
        private readonly IProviderService _providerService;

        public TestResultsService(ITestResultsRepository testResultsRepository,
            ISearchRepository<TestScenarioResultIndex> searchRepository,
            IMapper mapper,
            ILogger logger,
            ITelemetry telemetry,
            ITestRunnerResiliencePolicies policies,
            IProviderService providerService)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(testResultsRepository, nameof(testResultsRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(providerService, nameof(providerService));

            _testResultsRepository = testResultsRepository;
            _searchRepository = searchRepository;
            _mapper = mapper;
            _logger = logger;
            _telemetry = telemetry;
            _testResultsPolicy = policies.TestResultsRepository;
            _testResultsSearchPolicy = policies.TestResultsSearchRepository;
            _providerService = providerService;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth testResultsRepoHealth = await ((IHealthChecker)_testResultsRepository).IsHealthOk();
            (bool Ok, string Message) searchRepoHealth = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(TestEngineService)
            };
            health.Dependencies.AddRange(testResultsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        public async Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> testResults, IEnumerable<ProviderResult> providerResults)
        {
            Guard.ArgumentNotNull(testResults, nameof(testResults));

            if (!testResults.Any())
            {
                return HttpStatusCode.NotModified;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            Task<HttpStatusCode> repoUpdateTask = _testResultsPolicy.ExecuteAsync(() => _testResultsRepository.SaveTestProviderResults(testResults));

            IEnumerable<TestScenarioResultIndex> searchIndexItems = _mapper.Map<IEnumerable<TestScenarioResultIndex>>(testResults);

            foreach (TestScenarioResultIndex testScenarioResult in searchIndexItems)
            {
                ProviderResult providerResult = providerResults.FirstOrDefault(m => m.Provider.Id == testScenarioResult.ProviderId);

                if (providerResult != null)
                {
                    testScenarioResult.EstablishmentNumber = providerResult.Provider.EstablishmentNumber;
                    testScenarioResult.UKPRN = providerResult.Provider.UKPRN;
                    testScenarioResult.UPIN = providerResult.Provider.UPIN;
                    testScenarioResult.URN = providerResult.Provider.URN;
                    testScenarioResult.LocalAuthority = providerResult.Provider.Authority;
                    testScenarioResult.ProviderType = providerResult.Provider.ProviderType;
                    testScenarioResult.ProviderSubType = providerResult.Provider.ProviderSubType;
                    testScenarioResult.OpenDate = providerResult.Provider.DateOpened;
                }
            }

            Task<IEnumerable<IndexError>> searchUpdateTask = _testResultsSearchPolicy.ExecuteAsync(() => _searchRepository.Index(searchIndexItems));

            await TaskHelper.WhenAllAndThrow(searchUpdateTask, repoUpdateTask);

            IEnumerable<IndexError> indexErrors = searchUpdateTask.Result;
            HttpStatusCode repositoryUpdateStatusCode = repoUpdateTask.Result;

            stopwatch.Stop();

            if (!indexErrors.Any() && (repoUpdateTask.Result == HttpStatusCode.Created || repoUpdateTask.Result == HttpStatusCode.NotModified))
            {
                _telemetry.TrackEvent("UpdateTestScenario",
                        new Dictionary<string, string>() {
                            { "SpecificationId", testResults.First().Specification.Id }
                        },
                        new Dictionary<string, double>()
                        {
                            { "update-testscenario-elapsedMilliseconds", stopwatch.ElapsedMilliseconds },
                            { "update-testscenario-recordsUpdated", testResults.Count() },
                        }
                    );

                return HttpStatusCode.Created;
            }

            foreach (IndexError indexError in indexErrors)
            {
                _logger.Error($"SaveTestProviderResults index error {{key}}: {indexError.ErrorMessage}", indexError.Key);
            }

            if (repositoryUpdateStatusCode == default(HttpStatusCode))
            {
                _logger.Error("SaveTestProviderResults repository failed with no response code");
            }
            else
            {
                _logger.Error("SaveTestProviderResults repository failed with response code: {repositoryUpdateStatusCode}", repositoryUpdateStatusCode);

            }

            return HttpStatusCode.InternalServerError;
        }

        public async Task<IActionResult> ReIndex(HttpRequest request)
        {
            IEnumerable<DocumentEntity<TestScenarioResult>> testScenarioResults = await _testResultsRepository.GetAllTestResults();

            IEnumerable<ProviderSummary> summaries = await _providerService.FetchCoreProviderData();

            IList<TestScenarioResultIndex> searchItems = new List<TestScenarioResultIndex>();

            foreach (DocumentEntity<TestScenarioResult> documentEnity in testScenarioResults)
            {
                TestScenarioResult testScenarioResult = documentEnity.Content;

                TestScenarioResultIndex testScenarioResultIndex = new TestScenarioResultIndex
                {
                    TestResult = testScenarioResult.TestResult.ToString(),
                    SpecificationId = testScenarioResult.Specification.Id,
                    SpecificationName = testScenarioResult.Specification.Name,
                    TestScenarioId = testScenarioResult.TestScenario.Id,
                    TestScenarioName = testScenarioResult.TestScenario.Name,
                    ProviderName = testScenarioResult.Provider.Name,
                    ProviderId = testScenarioResult.Provider.Id,
                    LastUpdatedDate = documentEnity.UpdatedAt
                };

                ProviderSummary providerSummary = summaries.FirstOrDefault(m => m.Id == testScenarioResult.Provider.Id);

                if (providerSummary != null)
                {
                    testScenarioResultIndex.EstablishmentNumber = providerSummary.EstablishmentNumber;
                    testScenarioResultIndex.UKPRN = providerSummary.UKPRN;
                    testScenarioResultIndex.UPIN = providerSummary.UPIN;
                    testScenarioResultIndex.URN = providerSummary.URN;
                    testScenarioResultIndex.LocalAuthority = providerSummary.Authority;
                    testScenarioResultIndex.ProviderType = providerSummary.ProviderType;
                    testScenarioResultIndex.ProviderSubType = providerSummary.ProviderSubType;
                    testScenarioResultIndex.OpenDate = providerSummary.DateOpened;
                }

                searchItems.Add(testScenarioResultIndex);
            }

            for (int i = 0; i < searchItems.Count; i += 100)
            {
                IEnumerable<TestScenarioResultIndex> partitionedResults = searchItems.Skip(i).Take(100);

                IEnumerable<IndexError> errors = await _searchRepository.Index(partitionedResults);

                if (errors.Any())
                {
                    return new StatusCodeResult(500);
                }
            }

            return new NoContentResult();
        }

        public async Task UpdateTestResultsForSpecification(Message message)
        {
            SpecificationVersionComparisonModel specificationVersionComparison = message.GetPayloadAsInstanceOf<SpecificationVersionComparisonModel>();

            if (specificationVersionComparison == null || specificationVersionComparison.Current == null)
            {
                _logger.Error("A null specificationVersionComparison was provided to UpdateTestResultsForSpecification");

                throw new InvalidModelException(nameof(SpecificationVersionComparisonModel), new[] { "Null or invalid model provided" });
            }

            if (specificationVersionComparison.Current.Name == specificationVersionComparison.Previous.Name)
            {
                _logger.Information("No changes detected");
                return;
            }

            bool keepSearching = true;


            while (keepSearching)
            {
                SearchResults<TestScenarioResultIndex> results = await _searchRepository.Search("", new SearchParameters
                {
                    Skip = 0,
                    Top = 1000,
                    SearchMode = SearchMode.Any,
                    Filter = $"specificationId eq '{specificationVersionComparison.Id}' and specificationName ne '{specificationVersionComparison.Current.Name}'",
                    QueryType = QueryType.Full
                });

                if (results.Results.IsNullOrEmpty())
                {
                    keepSearching = false;
                }
                else
                {
                    IEnumerable<TestScenarioResultIndex> indexResults = results.Results.Select(m => m.Result);

                    if (results.Results.Count < 1000)
                    {
                        keepSearching = false;
                    }

                    foreach (TestScenarioResultIndex scenarioResultIndex in indexResults)
                    {
                        scenarioResultIndex.SpecificationName = specificationVersionComparison.Current.Name;
                    }

                    IEnumerable<IndexError> indexErrors = await _searchRepository.Index(indexResults);

                    if (indexErrors.Any())
                    {
                        _logger.Error($"The following errors occcurred while updating test results for specification id: {specificationVersionComparison.Id}, {string.Join(";", indexErrors.Select(m => m.ErrorMessage))}");
                    }
                }
            }
        }

        public async Task CleanupTestResultsForSpecificationProviders(Message message)
        {
            string specificationId = message.UserProperties["specificationId"].ToString();

            Models.Results.SpecificationProviders specificationProviders = message.GetPayloadAsInstanceOf<Models.Results.SpecificationProviders>();

            IEnumerable<TestScenarioResult> testScenarioResults = await _testResultsPolicy
                .ExecuteAsync(() => _testResultsRepository.GetCurrentTestResults(specificationProviders.Providers, specificationId)
            );

            if (testScenarioResults.Any())
            {
                _logger.Information($"Removing {specificationProviders.Providers.Count()} from test results for specification {specificationId}");

                await _testResultsPolicy.ExecuteAsync(() =>
                    _testResultsRepository.DeleteCurrentTestScenarioTestResults(testScenarioResults));

                SearchResults<TestScenarioResultIndex> indexItems = await _testResultsSearchPolicy
                    .ExecuteAsync(() => _searchRepository.Search(string.Empty, 
                            new SearchParameters
                            {
                                Top = testScenarioResults.Count(),
                                SearchMode = SearchMode.Any,
                                Filter = $"specificationId eq '{specificationId}' and (" + string.Join(" or ", testScenarioResults.Select(m => $"providerId eq '{m.Provider.Id}'")) + ")",
                                QueryType = QueryType.Full
                            }
                        )
                    );

                await _testResultsSearchPolicy.ExecuteAsync(() => _searchRepository.Remove(indexItems?.Results.Select(m => m.Result)));
            }
        }
    }
}
