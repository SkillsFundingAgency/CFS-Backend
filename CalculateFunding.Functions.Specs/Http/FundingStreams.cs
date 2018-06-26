using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Functions.Specs.Http
{
    public static class FundingStreams
    {
        [FunctionName("get-fundingstreams")]
        public static Task<IActionResult> RunGetFundingStreams(
           [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return svc.GetFundingStreams(req);
            }
        }

        [FunctionName("get-fundingstream-by-id")]
        public static Task<IActionResult> RunGetFundingStreamById(
           [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return svc.GetFundingStreamById(req);
            }
        }

        [FunctionName("save-fundingStream")]
        public static Task<IActionResult> RunSaveFundingStreamn(
           [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return svc.SaveFundingStream(req);
            }
        }

        [FunctionName("get-fundingstreams-for-specification")]
        public static Task<IActionResult> RunGetFundingStreamsForSpecificationById(
           [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

                return svc.GetFundingStreamsForSpecificationById(req);
            }
        }
    }

}
