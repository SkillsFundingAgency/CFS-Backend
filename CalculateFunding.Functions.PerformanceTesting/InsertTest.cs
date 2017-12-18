using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.DataImporter;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Functions.Common;
using CalculateFunding.Repositories.Common.Cosmos;

namespace CalculateFunding.Functions.PerformanceTesting
{
    public static class InsertTest
    {
        [FunctionName("InsertTest")]
        public static async System.Threading.Tasks.Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log)
        {

            req.Query.TryGetValue("count", out var countParam);
            req.Query.TryGetValue("requestUnits", out var ruParam);

            var stopwatch = new Stopwatch();

            var list = new List<AptProviderInformation>();
            var count = countParam.Select(x => (int?)int.Parse(x)).FirstOrDefault() ?? 1000;
            var requestUnits = ruParam.Select(x => (int?)int.Parse(x)).FirstOrDefault() ?? 1000;
            var repository = ServiceFactory.GetService<CosmosRepository>();



                await repository.EnsureCollectionExists();
            for (var i = 0; i < count; i++)
            {

                var providerInformation = new AptProviderInformation
                {
                    BudgetId = "budget-gag-1718",
                    DatasetName = "APT Provider Information",
                    ProviderUrn = $"{i:00000000}",
                    DateOpened = DateTime.Today.AddDays(-1 * i),
                    LocalAuthority = $"Local Authority {i}",
                    ProviderName = $"Provider {i}",
                    UPIN = $"{i:00000000}",
                    Phase = "Primary"

                };
                list.Add(providerInformation);


            }

            stopwatch.Start();

            await repository.BulkCreateAsync(list, requestUnits / 1000);

            stopwatch.Stop();


            var itemsPerSec = count / (stopwatch.ElapsedMilliseconds / 1000M);
            log.LogInformation($"Added {count} items in {stopwatch.ElapsedMilliseconds}ms");
            return new JsonResult(new
            {
                stopwatch.ElapsedMilliseconds,
                count,
                itemsPerSec
            });
        }
    }
}
