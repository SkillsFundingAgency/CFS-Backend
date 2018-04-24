using CalculateFunding.Models;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class TestResultsCountsService : ITestResultsCountsService
    {
        private readonly ITestResultsSearchService _testResultsService;
        private readonly ILogger _logger;

        public TestResultsCountsService(ITestResultsSearchService testResultsService, ILogger logger)
        {
            Guard.ArgumentNotNull(testResultsService, nameof(testResultsService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            _testResultsService = testResultsService;
            _logger = logger;
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

            IList<TestScenarioResultCounts> resultCounts = new List<TestScenarioResultCounts>();

            foreach (string testScenarioId in requestModel.TestScenarioIds)
            {
                SearchModel searchModel = new SearchModel
                {
                    IncludeFacets = true,
                    Top = 1,
                    PageNumber = 1,
                    Filters = new Dictionary<string, string[]> { { "testScenarioId", new[] { testScenarioId } } }
                };

                TestScenarioSearchResults result = await _testResultsService.SearchTestScenarioResults(searchModel);

                if (result != null)
                {

                    Facet facet = result.Facets?.FirstOrDefault(m => m.Name == "testResult");

                    if (facet != null)
                    {
                        FacetValue passedValue = facet.FacetValues.FirstOrDefault(m => m.Name == "Passed");
                        FacetValue failedValue = facet.FacetValues.FirstOrDefault(m => m.Name == "Failed");
                        FacetValue ignoredValue = facet.FacetValues.FirstOrDefault(m => m.Name == "Ignored");

                        if (result.Results.Any())
                        {
                            resultCounts.Add(new TestScenarioResultCounts
                            {
                                TestScenarioName = result.Results.FirstOrDefault().TestScenarioName,
                                TestScenarioId = testScenarioId,
                                Passed = passedValue != null ? passedValue.Count : 0,
                                Failed = failedValue != null ? failedValue.Count : 0,
                                Ignored = ignoredValue != null ? ignoredValue.Count : 0,
                                LastUpdatedDate = result.Results.FirstOrDefault().LastUpdatedDate
                            });
                        }


                    }
                }

            }

            return new OkObjectResult(resultCounts);
        }
    }
}
