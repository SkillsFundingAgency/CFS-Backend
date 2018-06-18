using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Results.Http
{
    public static class Providers
    {
        [FunctionName("providers-search")]
        public static Task<IActionResult> RunSearchDataDefinitions(
         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (IServiceScope scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsSearchService svc = scope.ServiceProvider.GetService<IResultsSearchService>();

                return svc.SearchProviders(req);
            }
        }

        [FunctionName("get-provider-results")]
        public static Task<IActionResult> RunGetProviderResults(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.GetProviderResults(req);
            }
        }

	    [FunctionName("get-provider-specs")]
	    public static Task<IActionResult> RunGetProviderSpecs(
		    [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
	    {
		    using (var scope = IocConfig.Build().CreateHttpScope(req))
		    {
			    IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

			    return svc.GetProviderSpecifications(req);
		    }
	    }

        [FunctionName("get-provider")]
        public static Task<IActionResult> RunGetProvider(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.GetProviderById(req);
            }
        }

        [FunctionName("update-provider-source-dataset")]
        public static Task<IActionResult> RunUpdateProviderSourceDataset(
           [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.UpdateProviderSourceDataset(req);
            }
        }

        [FunctionName("get-provider-source-datasets")]
        public static Task<IActionResult> RunGetProviderSourceDatasetsByProviderIdAndSpecificationId(
           [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.GetProviderSourceDatasetsByProviderIdAndSpecificationId(req);
            }
        }

        [FunctionName("get-scoped-providerids")]
        public static Task<IActionResult> RunGetScopedproviderIds(
           [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.GetScopedProviderIdsBySpecificationId(req);
            }
        }

        [FunctionName("reindex-calc-provider-results")]
        public static Task<IActionResult> RunReIndexCalculationProviderResults(
          [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.ReIndexCalculationProviderResults();
            }
        }

        [FunctionName("calculation-provider-results-search")]
        public static Task<IActionResult> RunCalculationProviderResultsSearch(
           [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ICalculationProviderResultsSearchService svc = scope.ServiceProvider.GetService<ICalculationProviderResultsSearchService>();

                return svc.SearchCalculationProviderResults(req);
            }
        }

        [FunctionName("get-calculation-result-totals-for-specifications")]
        public static Task<IActionResult> RunGetFundingCalculationResultsForSpecifications(
          [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.GetFundingCalculationResultsForSpecifications(req);
            }
        }

        [FunctionName("publish-provider-results")]
        public static Task<IActionResult> RunPublishProviderResults(
          [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.PublishProviderResults(req);
            }
        }

        [FunctionName("get-published-provider-results-for-specification")]
        public static Task<IActionResult> RunGetPublishedProviderResultsForSpecification(
         [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.GetPublishedProviderResultsBySpecificationId(req);
            }
        }

        [FunctionName("update-published-allocationline-results-status")]
        public static Task<IActionResult> RunUpdatePublishedAllocationLineResultsStatus(
          [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.UpdatePublishedAllocationLineResultsStatus(req);
            }
        }

        [FunctionName("get-specification-provider-results")]
        public static Task<IActionResult> RunGetSpecificationProviderResults(
           [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IResultsService svc = scope.ServiceProvider.GetService<IResultsService>();

                return svc.GetProviderResultsBySpecificationId(req);
            }
        }
    }
}
