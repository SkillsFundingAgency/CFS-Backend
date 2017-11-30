
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using CalculateFunding.Repository;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.DataImporter;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace CalculateFunding.Functions.PerformanceTesting
{
    public static class InsertTest
    {
        [FunctionName("InsertTest")]
        public static async System.Threading.Tasks.Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log)
        {

            req.Query.TryGetValue("count", out var countParam);
            req.Query.TryGetValue("requestUnits", out var ruParam);
            req.Query.TryGetValue("partitionKey", out var partitionKeyParam);
            req.Query.TryGetValue("parallelism", out var parallelismKeyParam);

            var stopwatch = new Stopwatch();

            var list = new List<AptProviderInformation>();
            var count = countParam.Select(x => (int?)int.Parse(x)).FirstOrDefault() ?? 1000;
            var requestUnits = ruParam.Select(x => (int?)int.Parse(x)).FirstOrDefault() ?? 1000;
            using (var repository = new Repository<ProviderSourceDataset>("datasets", "/providerUrn"))
            {
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
                        Phase =  "Primary"

                    };
                    list.Add(providerInformation);


                }

                stopwatch.Start();

                int taskCount;
                int degreeOfParallelism = parallelismKeyParam.Select(x => (int?)int.Parse(x)).FirstOrDefault() ?? -1;

                if (degreeOfParallelism == -1)
                {
                    // set TaskCount = 10 for each 10k RUs, minimum 1, maximum 250
                    taskCount = Math.Max(requestUnits / 1000, 1);
                    taskCount = Math.Min(taskCount, 250);
                }
                else
                {
                    taskCount = degreeOfParallelism;
                }

                await Task.Run(() => Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism}, (item) =>
                {
                    Task.WaitAll(repository.CreateAsync(item));
                }));


                stopwatch.Stop();

            }
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
