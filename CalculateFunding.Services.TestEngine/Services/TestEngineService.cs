using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.TestEngine.Interfaces;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class TestEngineService : ITestEngineService, IHealthChecker
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly ILogger _logger;
        private readonly ITestEngine _testEngine;
        private readonly IScenariosRepository _scenariosRepository;
        private readonly IProviderSourceDatasetsRepository _providerSourceDatasetsRepository;
        private readonly ITestResultsService _testResultsService;
        private readonly ITestResultsRepository _testResultsRepository;
        private readonly ICalculationsApiClient _calcsApiClient;
        private readonly ITelemetry _telemetry;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IMapper _mapper;

        private readonly AsyncPolicy _cacheProviderPolicy;
        private readonly AsyncPolicy _scenariosRepositoryPolicy;
        private readonly AsyncPolicy _providerSourceDatasetsRepositoryPolicy;
        private readonly AsyncPolicy _testResultsRepositoryPolicy;
        private readonly AsyncPolicy _calcsApiClientPolicy;
        private readonly AsyncPolicy _specificationsApiClientPolicy;

        public TestEngineService(
            ICacheProvider cacheProvider,
            ISpecificationsApiClient specificationsApiClient,
            ILogger logger,
            ITestEngine testEngine,
            IScenariosRepository scenariosRepository,
            IProviderSourceDatasetsRepository providerSourceDatasetsRepository,
            ITestResultsService testResultsService,
            ITestResultsRepository testResultsRepository,
            ICalculationsApiClient calcsApiClient,
            ITelemetry telemetry,
            ITestRunnerResiliencePolicies resiliencePolicies,
            ICalculationsRepository calculationsRepository,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(testEngine, nameof(testEngine));
            Guard.ArgumentNotNull(scenariosRepository, nameof(scenariosRepository));
            Guard.ArgumentNotNull(providerSourceDatasetsRepository, nameof(providerSourceDatasetsRepository));
            Guard.ArgumentNotNull(testResultsService, nameof(testResultsService));
            Guard.ArgumentNotNull(testResultsRepository, nameof(testResultsRepository));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(resiliencePolicies?.CacheProviderRepository, nameof(resiliencePolicies.CacheProviderRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ScenariosRepository, nameof(resiliencePolicies.ScenariosRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.ProviderSourceDatasetsRepository, nameof(resiliencePolicies.ProviderSourceDatasetsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.TestResultsRepository, nameof(resiliencePolicies.TestResultsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsApiClient, nameof(resiliencePolicies.CalculationsApiClient)); ;
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(calcsApiClient, nameof(calcsApiClient));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            
            _cacheProvider = cacheProvider;
            _specificationsApiClient = specificationsApiClient;
            _logger = logger;
            _testEngine = testEngine;
            _scenariosRepository = scenariosRepository;
            _providerSourceDatasetsRepository = providerSourceDatasetsRepository;
            _testResultsService = testResultsService;
            _testResultsRepository = testResultsRepository;
            _telemetry = telemetry;
            _mapper = mapper;

            _cacheProviderPolicy = resiliencePolicies.CacheProviderRepository;
            _scenariosRepositoryPolicy = resiliencePolicies.ScenariosRepository;
            _providerSourceDatasetsRepositoryPolicy = resiliencePolicies.ProviderSourceDatasetsRepository;
            _testResultsRepositoryPolicy = resiliencePolicies.TestResultsRepository;
            _calcsApiClientPolicy = resiliencePolicies.CalculationsApiClient;
            _calcsApiClient = calcsApiClient;
            _calculationsRepository = calculationsRepository;
            _specificationsApiClientPolicy = resiliencePolicies.SpecificationsApiClient;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cacheHealth = await _cacheProvider.IsHealthOk();
            ServiceHealth testEngineHealth = await ((IHealthChecker)_testEngine).IsHealthOk();
            ServiceHealth scenariosRepoHealth = await ((IHealthChecker)_scenariosRepository).IsHealthOk();
            ServiceHealth providerSourceRepoHealth = await ((IHealthChecker)_providerSourceDatasetsRepository).IsHealthOk();
            ServiceHealth testResultsRepoHealth = await ((IHealthChecker)_testResultsRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(TestEngineService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });
            health.Dependencies.AddRange(testEngineHealth.Dependencies);
            health.Dependencies.AddRange(scenariosRepoHealth.Dependencies);
            health.Dependencies.AddRange(providerSourceRepoHealth.Dependencies);
            health.Dependencies.AddRange(testResultsRepoHealth.Dependencies);

            return health;
        }

        public async Task RunTests(Message message)
        {
            Stopwatch runTestsStopWatch = Stopwatch.StartNew();

            string specificationId = message.UserProperties["specificationId"].ToString();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("Null or empty specification id provided");
                return;
            }

            BuildProject buildProject = _mapper.Map<BuildProject>(await _calcsApiClientPolicy.ExecuteAsync(() => _calcsApiClient.GetBuildProjectBySpecificationId(specificationId)));

            if (buildProject == null)
            {
                _logger.Error("A null build project was provided to UpdateAllocations");

                throw new ArgumentNullException(nameof(buildProject));
            }

            string cacheKey = message.UserProperties["providerResultsCacheKey"].ToString();

            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                _logger.Error("Null or empty cache key provided");
                return;
            }

            Stopwatch providerResultsQueryStopwatch = Stopwatch.StartNew();
            IEnumerable<ProviderResult> providerResults = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<List<ProviderResult>>($"{CacheKeys.ProviderResultBatch}{cacheKey}"));
            providerResultsQueryStopwatch.Stop();

            if (providerResults.IsNullOrEmpty())
            {
                _logger.Error($"No provider results found in cache for key: {cacheKey}");
                return;
            }

            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<ProviderResult>>($"{CacheKeys.ProviderResultBatch}{cacheKey}"));

            Stopwatch testScenariosStopwatch = Stopwatch.StartNew();
            IEnumerable<TestScenario> testScenarios = await _scenariosRepositoryPolicy.ExecuteAsync(() => _scenariosRepository.GetTestScenariosBySpecificationId(specificationId));
            testScenariosStopwatch.Stop();

            if (testScenarios.IsNullOrEmpty())
            {
                _logger.Warning($"No test scenarios found for specification id: {specificationId}");
                return;
            }

            Stopwatch specificationLookupStopwatch = Stopwatch.StartNew();
            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));
            specificationLookupStopwatch.Stop();

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                _logger.Error($"No specification found for specification id: {specificationId}");
                return;
            }

            SpecModel.SpecificationSummary specification = specificationApiResponse.Content;

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id);

            Stopwatch providerSourceDatasetsStopwatch = Stopwatch.StartNew();
            IEnumerable<ProviderSourceDataset> sourceDatasets = await _providerSourceDatasetsRepositoryPolicy.ExecuteAsync(() =>
                                                     _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(providerIds, specificationId));

            providerSourceDatasetsStopwatch.Stop();

            if (sourceDatasets.IsNullOrEmpty())
            {
                _logger.Error($"No source datasets found for specification id: {specificationId}");
                return;
            }

            byte[] assembly = await _calculationsRepository.GetAssemblyBySpecificationId(specificationId);
            if (assembly.IsNullOrEmpty())
            {
                _logger.Error($"No assemblyfor specification id: {specificationId}");
                return;
            }

            buildProject.Build.Assembly = assembly;

            Stopwatch existingTestResultsStopwatch = Stopwatch.StartNew();
            IEnumerable<TestScenarioResult> testScenarioResults = await _testResultsRepositoryPolicy.ExecuteAsync(() => _testResultsRepository.GetCurrentTestResults(providerIds, specificationId));
            existingTestResultsStopwatch.Stop();

            Stopwatch runTestsStopwatch = Stopwatch.StartNew();
            IEnumerable<TestScenarioResult> results = await _testEngine.RunTests(testScenarios, providerResults, sourceDatasets, testScenarioResults.ToList(), specification, buildProject);
            runTestsStopwatch.Stop();

            Stopwatch saveResultsStopwatch = new Stopwatch();
            if (results.Any())
            {
                saveResultsStopwatch.Start();
                HttpStatusCode status = await _testResultsService.SaveTestProviderResults(results, providerResults);
                saveResultsStopwatch.Stop();

                if (!status.IsSuccess())
                {
                    _logger.Error($"Failed to save test results with status code: {status}");
                }
            }

            runTestsStopWatch.Stop();

            IDictionary<string, double> metrics = new Dictionary<string, double>()
                    {
                        { "tests-run-totalMs", runTestsStopWatch.ElapsedMilliseconds },
                        { "tests-run-testScenarioQueryMs", testScenariosStopwatch.ElapsedMilliseconds },
                        { "tests-run-numberOfTestScenarios", testScenarios.Count() },
                        { "tests-run-providersResultsQueryMs", providerResultsQueryStopwatch.ElapsedMilliseconds},
                        { "tests-run-totalProvidersProcessed", providerIds.Count() },
                        { "tests-run-specificationQueryMs", specificationLookupStopwatch.ElapsedMilliseconds },
                        { "tests-run-providerSourceDatasetsQueryMs", providerSourceDatasetsStopwatch.ElapsedMilliseconds },
                        { "tests-run-existingTestsQueryMs", existingTestResultsStopwatch.ElapsedMilliseconds },
                        { "tests-run-existingTestScenarioResultsTotal",testScenarioResults.Count() },
                        { "tests-run-runTestsMs", runTestsStopwatch.ElapsedMilliseconds },
                    };

            if (results.Any())
            {
                metrics.Add("tests-run-saveTestResultsMs", saveResultsStopwatch.ElapsedMilliseconds);
                metrics.Add("tests-run-numberOfSavedResults", results.Count());
            }

            _telemetry.TrackEvent("RunTests",
                new Dictionary<string, string>()
                {
                        { "specificationId" , specificationId },
                        { "buildProjectId" , buildProject.Id },
                        { "cacheKey" , cacheKey },
                },
                metrics
            );
        }

        public async Task<IActionResult> RunTests(TestExecutionModel testExecutionModel)
        {
            BuildProject buildProject = testExecutionModel.BuildProject;

            if (buildProject == null)
            {
                _logger.Error("Null build project provided");
                return new BadRequestObjectResult("Null build project provided");
            }

            string specificationId = buildProject.SpecificationId;

            IEnumerable<ProviderResult> providerResults = testExecutionModel.ProviderResults;

            if (providerResults.IsNullOrEmpty())
            {
                _logger.Error("No provider results were provided");
                return new BadRequestObjectResult("No provider results were provided");
            }

            IEnumerable<TestScenario> testScenarios = await _scenariosRepository.GetTestScenariosBySpecificationId(specificationId);

            if (testScenarios.IsNullOrEmpty())
            {
                _logger.Warning($"No test scenarios found for specification id: {specificationId}");
                return new PreconditionFailedResult(string.Empty);
            }

            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                _logger.Error($"No specification found for specification id: {specificationId}");
                return new PreconditionFailedResult(string.Empty);
            }

            SpecModel.SpecificationSummary specification = specificationApiResponse.Content;

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id).ToList();

            IEnumerable<ProviderSourceDataset> sourceDatasets = await _providerSourceDatasetsRepositoryPolicy.ExecuteAsync(() =>
                                                     _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(providerIds, specificationId));

            if (sourceDatasets.IsNullOrEmpty())
            {
                _logger.Error($"No source datasets found for specification id: {specificationId}");
                return new PreconditionFailedResult(string.Empty);
            }

            IEnumerable<TestScenarioResult> testScenarioResults = await _testResultsRepository.GetCurrentTestResults(providerIds, specificationId);

            IEnumerable<TestScenarioResult> results = await _testEngine.RunTests(testScenarios, providerResults, sourceDatasets, testScenarioResults.ToList(), specification, buildProject);


            if (results.Any())
            {
                HttpStatusCode status = await _testResultsService.SaveTestProviderResults(results, providerResults);

                if (!status.IsSuccess())
                {
                    _logger.Error($"Failed to save test results with status code: {status}");
                }
            }

            return new OkObjectResult(results);
        }

    }
}
