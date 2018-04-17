using AutoMapper;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class TestResultsService : ITestResultsService
    {
        private readonly ITestResultsRepository _testResultsRepository;
        private readonly ISearchRepository<TestScenarioResultIndex> _searchRepository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly Policy _testResultsPolicy;
        private readonly Policy _testResultsSearchPolicy;

        public TestResultsService(ITestResultsRepository testResultsRepository,
            ISearchRepository<TestScenarioResultIndex> searchRepository,
            IMapper mapper,
            ILogger logger,
            ITelemetry telemetry,
            ITestRunnerResiliencePolicies policies)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(testResultsRepository, nameof(testResultsRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(policies, nameof(policies));

            _testResultsRepository = testResultsRepository;
            _searchRepository = searchRepository;
            _mapper = mapper;
            _logger = logger;
            _telemetry = telemetry;

            _testResultsPolicy = policies.TestResultsRepository;
            _testResultsSearchPolicy = policies.TestResultsSearchRepository;
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

            foreach(TestScenarioResultIndex testScenarioResult in searchIndexItems)
            {
                ProviderResult providerResult = providerResults.FirstOrDefault(m => m.Provider.Id == testScenarioResult.ProviderId);

                if(providerResult != null)
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

        public Task<IActionResult> Reindex(HttpRequest request)
        {
            return Task.FromResult<IActionResult>(new OkResult());
        }
    }
}
