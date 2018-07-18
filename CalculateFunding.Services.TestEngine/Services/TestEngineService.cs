using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Caching;
using System;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class TestEngineService : ITestEngineService
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly ISpecificationRepository _specificationRepository;
        private readonly ILogger _logger;
        private readonly ITestEngine _testEngine;
        private readonly IScenariosRepository _scenariosRepository;
        private readonly IProviderSourceDatasetsRepository _providerSourceDatasetsRepository;
        private readonly ITestResultsService _testResultsService;
        private readonly ITestResultsRepository _testResultsRepository;
        private readonly IBuildProjectRepository _buildProjectRepository;
        private readonly ITelemetry _telemetry;

        private readonly Polly.Policy _cacheProviderPolicy;
        private readonly Polly.Policy _specificationRepositoryPolicy;
        private readonly Polly.Policy _scenariosRepositoryPolicy;
        private readonly Polly.Policy _providerSourceDatasetsRepositoryPolicy;
        private readonly Polly.Policy _testResultsRepositoryPolicy;
        private readonly Polly.Policy _builProjectsRepositoryPolicy;
       

        public TestEngineService(
            ICacheProvider cacheProvider,
            ISpecificationRepository specificationRepository,
            ILogger logger,
            ITestEngine testEngine,
            IScenariosRepository scenariosRepository,
            IProviderSourceDatasetsRepository providerSourceDatasetsRepository,
            ITestResultsService testResultsService,
            ITestResultsRepository testResultsRepository,
            IBuildProjectRepository buildProjectRepository,
            ITelemetry telemetry,
            ITestRunnerResiliencePolicies resiliencePolicies)
        {
            _cacheProvider = cacheProvider;
            _specificationRepository = specificationRepository;
            _logger = logger;
            _testEngine = testEngine;
            _scenariosRepository = scenariosRepository;
            _providerSourceDatasetsRepository = providerSourceDatasetsRepository;
            _testResultsService = testResultsService;
            _testResultsRepository = testResultsRepository;
            _telemetry = telemetry;

            _cacheProviderPolicy = resiliencePolicies.CacheProviderRepository;
            _specificationRepositoryPolicy = resiliencePolicies.SpecificationRepository;
            _scenariosRepositoryPolicy = resiliencePolicies.ScenariosRepository;
            _providerSourceDatasetsRepositoryPolicy = resiliencePolicies.ProviderSourceDatasetsRepository;
            _testResultsRepositoryPolicy = resiliencePolicies.TestResultsRepository;
            _builProjectsRepositoryPolicy = resiliencePolicies.BuildProjectRepository;
            _buildProjectRepository = buildProjectRepository;
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

            BuildProject buildProject = await _builProjectsRepositoryPolicy.ExecuteAsync(() => _buildProjectRepository.GetBuildProjectBySpecificationId(specificationId));

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
            SpecificationSummary specification = await _specificationRepositoryPolicy.ExecuteAsync(() => _specificationRepository.GetSpecificationSummaryById(specificationId));
            specificationLookupStopwatch.Stop();

            if (specification == null)
            {
                _logger.Error($"No specification found for specification id: {specificationId}");
                return;
            }

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id);

            Stopwatch providerSourceDatasetsStopwatch = Stopwatch.StartNew();
            IEnumerable<ProviderSourceDatasetCurrent> sourceDatasets = await _providerSourceDatasetsRepositoryPolicy.ExecuteAsync(() =>
                                                     _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(providerIds, specificationId));

            providerSourceDatasetsStopwatch.Stop();

            if (sourceDatasets.IsNullOrEmpty())
            {
                _logger.Error($"No source datasets found for specification id: {specificationId}");
                return;
            }

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
                    _logger.Error($"Failed to save test results with status code: {status.ToString()}");
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

        public async Task<IActionResult> RunTests(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            TestExecutionModel testExecutionModel = JsonConvert.DeserializeObject<TestExecutionModel>(json);

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
                return new StatusCodeResult(412);
            }

            SpecificationSummary specification = await _specificationRepository.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                _logger.Error($"No specification found for specification id: {specificationId}");
                return new StatusCodeResult(412);
            }

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id).ToList();

            IEnumerable<ProviderSourceDatasetCurrent> sourceDatasets = await _providerSourceDatasetsRepositoryPolicy.ExecuteAsync(() =>
                                                     _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndSpecificationId(providerIds, specificationId));

            if (sourceDatasets.IsNullOrEmpty())
            {
                _logger.Error($"No source datasets found for specification id: {specificationId}");
                return new StatusCodeResult(412);
            }

            IEnumerable<TestScenarioResult> testScenarioResults = await _testResultsRepository.GetCurrentTestResults(providerIds, specificationId);

            IEnumerable<TestScenarioResult> results = await _testEngine.RunTests(testScenarios, providerResults, sourceDatasets, testScenarioResults.ToList(), specification, buildProject);

          
            if (results.Any())
            {
               
                HttpStatusCode status = await _testResultsService.SaveTestProviderResults(results, providerResults);
                
                if (!status.IsSuccess())
                {
                    _logger.Error($"Failed to save test results with status code: {status.ToString()}");
                }
            }

            return new OkObjectResult(results);
        }
    }
}
