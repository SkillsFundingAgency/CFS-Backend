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
    public static class FundingPeriods
    {
        //[FunctionName("get-fundingperiods")]
        //public static Task<IActionResult> RunGetFundingStreams(
        //   [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        //{
        //    using (var scope = IocConfig.Build().CreateHttpScope(req))
        //    {
        //        ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

        //        return svc.GetFundingPeriods(req);
        //    }
        //}

        //[FunctionName("save-fundingperiods")]
        //public static Task<IActionResult> RunSaveFundingStreamn(
        //   [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        //{
        //    using (var scope = IocConfig.Build().CreateHttpScope(req))
        //    {
        //        ISpecificationsService svc = scope.ServiceProvider.GetService<ISpecificationsService>();

        //        return svc.SaveFundingPeriods(req);
        //    }
        //}
    }

}
