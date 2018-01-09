using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

        [FunctionName("funding-streams-commands")]
        public static async Task<IActionResult> RunCommands(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            var restMethods = new RestCommandMethods<FundingStream, FundingStreamCommand>("spec-events");
            return await restMethods.Run(req, log);
        }
    }

}
