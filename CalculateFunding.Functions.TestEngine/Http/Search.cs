using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.TestEngine.Http
{
    public static class Search
    {
        [FunctionName("testscenario-search")]
        public static Task<IActionResult> RunSearchTestScenarioResults(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ITestResultsSearchService svc = scope.ServiceProvider.GetService<ITestResultsSearchService>();

                return svc.SearchTestScenarioResults(req);
            }
        }

        [FunctionName("testscenario-reindex")]
        public static Task<IActionResult> RunSearchReindex(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ITestResultsService svc = scope.ServiceProvider.GetService<ITestResultsService>();

                return svc.ReIndex(req);
            }
        }

        [FunctionName("get-result-counts")]
        public static Task<IActionResult> RunGetResultCounts(
       [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ITestResultsCountsService svc = scope.ServiceProvider.GetService<ITestResultsCountsService>();

                return svc.GetResultCounts(req);
            }
        }
    }
}
