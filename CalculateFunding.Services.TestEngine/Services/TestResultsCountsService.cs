using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class TestResultsCountsService : ITestResultsCountsService, IHealthChecker
    {
        private readonly ITestResultsSearchService _testResultsService;
        private readonly ITestResultsRepository _testResultsRepository;
        private readonly ILogger _logger;

        public TestResultsCountsService(ITestResultsSearchService testResultsService, ITestResultsRepository testResultsRepository, ILogger logger)
        {
            Guard.ArgumentNotNull(testResultsService, nameof(testResultsService));
            Guard.ArgumentNotNull(testResultsRepository, nameof(testResultsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _testResultsService = testResultsService;
            _testResultsRepository = testResultsRepository;
            _logger = logger;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth testResultsRepoHealth = await ((IHealthChecker)_testResultsRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(TestResultsCountsService)
            };
            health.Dependencies.AddRange(testResultsRepoHealth.Dependencies);

            return health;
        }

        public async Task<IActionResult> GetResultCounts(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            TestScenariosResultsCountsRequestModel requestModel = JsonConvert.DeserializeObject<TestScenariosResultsCountsRequestModel>(json);

            if (requestModel == null || requestModel.TestScenarioIds.IsNullOrEmpty())
            {
                _logger.Error("Null or empty test scenario ids provided");

                return new BadRequestObjectResult("Null or empty test scenario ids provided");
            }

            ConcurrentBag<TestScenarioResultCounts> resultCounts = new ConcurrentBag<TestScenarioResultCounts>();

            Parallel.ForEach(requestModel.TestScenarioIds, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, testScenarioId =>
            {
                SearchModel searchModel = new SearchModel
                {
                    IncludeFacets = true,
                    Top = 1,
                    PageNumber = 1,
                    OverrideFacetFields = new string[] { "testResult" },
                    Filters = new Dictionary<string, string[]> { { "testScenarioId", new[] { testScenarioId } } }
                };

                TestScenarioSearchResults result = _testResultsService.SearchTestScenarioResults(searchModel).Result;

                if (result != null && !result.Results.IsNullOrEmpty())
                {
                    Facet facet = result.Facets?.FirstOrDefault(m => m.Name == "testResult");

                    if (facet != null)
                    {
                        FacetValue passedValue = facet.FacetValues.FirstOrDefault(m => m.Name == "Passed");
                        FacetValue failedValue = facet.FacetValues.FirstOrDefault(m => m.Name == "Failed");
                        FacetValue ignoredValue = facet.FacetValues.FirstOrDefault(m => m.Name == "Ignored");

                        resultCounts.Add(new TestScenarioResultCounts
                        {
                            TestScenarioName = result.Results.First().TestScenarioName,
                            TestScenarioId = testScenarioId,
                            Passed = passedValue != null ? passedValue.Count : 0,
                            Failed = failedValue != null ? failedValue.Count : 0,
                            Ignored = ignoredValue != null ? ignoredValue.Count : 0,
                            LastUpdatedDate = result.Results.First().LastUpdatedDate
                        });
                    }
                }
            });

            return new OkObjectResult(resultCounts);
        }

        public async Task<IActionResult> GetTestScenarioCountsForProvider(HttpRequest request)
        {
            request.Query.TryGetValue("providerId", out var providerIdParse);

            var providerId = providerIdParse.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(providerId))
            {
                _logger.Error($"No providerId was provided to {nameof(GetTestScenarioCountsForProvider)}");

                return new BadRequestObjectResult("Null or empty providerId provided");
            }

            ProviderTestScenarioResultCounts result = await _testResultsRepository.GetProviderCounts(providerId);

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetTestScenarioCountsForProviderForSpecification(HttpRequest request)
        {
            request.Query.TryGetValue("providerId", out var providerIdParse);

            string providerId = providerIdParse.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(providerId))
            {
                _logger.Error($"No providerId was provided to {nameof(GetTestScenarioCountsForProviderForSpecification)}");

                return new BadRequestObjectResult("Null or empty providerId provided");
            }

            request.Query.TryGetValue("specificationId", out var specificationIdParse);

            string specificationId = specificationIdParse.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error($"No specificationId was provided to {nameof(GetTestScenarioCountsForProviderForSpecification)}");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }

            ScenarioResultCounts result = await _testResultsRepository.GetProvideCountForSpecification(providerId, specificationId);

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetTestScenarioCountsForSpecifications(HttpRequest request)
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

            ConcurrentBag<SpecificationTestScenarioResultCounts> scenarioCountModels = new ConcurrentBag<SpecificationTestScenarioResultCounts>();

            IList<Task> scenarioCountsTasks = new List<Task>();

            foreach (string specificationId in specifications.SpecificationIds)
            {
                scenarioCountsTasks.Add(Task.Run(async () =>
                {
                    SpecificationTestScenarioResultCounts scenarioResultCounts = await _testResultsRepository.GetSpecificationCounts(specificationId);

                    scenarioCountModels.Add(scenarioResultCounts);

                }));
            }

            try
            {
                await TaskHelper.WhenAllAndThrow(scenarioCountsTasks.ToArray());
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult($"An error occurred when obtaining scenario counts with the follwing message: \n {ex.Message}");
            }

            return new OkObjectResult(scenarioCountModels);
        }
    }
}
