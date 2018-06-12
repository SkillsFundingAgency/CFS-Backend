using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.TestEngine.Http
{
    public static class ResultCounts
    {
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

        [FunctionName("get-testscenario-result-counts-for-specifications")]
        public static Task<IActionResult> RunGetTestScenarioCountsForSpecifications(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ITestResultsCountsService svc = scope.ServiceProvider.GetService<ITestResultsCountsService>();

                return svc.GetTestScenarioCountsForSpecifications(req);
            }
        }

        [FunctionName("get-testscenario-result-counts-for-specification-for-provider")]
        public static Task<IActionResult> RunGetTestScenarioCountsForProviderForSpecification(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ITestResultsCountsService svc = scope.ServiceProvider.GetService<ITestResultsCountsService>();

                return svc.GetTestScenarioCountsForProviderForSpecification(req);
            }
        }
    }
}
