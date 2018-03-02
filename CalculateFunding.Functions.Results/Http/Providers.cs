using System.Net.Http;
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
    }
}
