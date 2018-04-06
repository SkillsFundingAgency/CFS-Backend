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

namespace CalculateFunding.Functions.Specs.Http
{
    public static class FundingStreams
    {
        [FunctionName("funding-streams")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req, ILogger log)
        {
            IServiceProvider provider = IocConfig.Build();

            ISpecificationsService svc = provider.GetService<ISpecificationsService>();
            return svc.GetFundingStreams(req);
        }
    }

}
