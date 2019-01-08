﻿using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Results.Messages;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Results
    {
        [FunctionName("on-provider-results-published")]
        public static async Task RunPublishProviderResults([QueueTrigger(ServiceBusConstants.QueueNames.PublishProviderResults, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnProviderResultsPublishedEvent.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-fetch-provider-profile")]
        public static async Task RunFetchProviderProfile([QueueTrigger(ServiceBusConstants.QueueNames.FetchProviderProfile, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<IEnumerable<FetchProviderProfilingMessageItem>>(item);

            await Functions.Results.ServiceBus.OnFetchProviderProfileEvent.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-migrate-result-versions")]
        public static async Task RunMigrateResultVersions([QueueTrigger(ServiceBusConstants.QueueNames.MigrateResultVersions, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnMigrateResultVersionsEvent.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-migrate-feed-index-id")]
        public static async Task RunMigrateResultFeedIndexes([QueueTrigger(ServiceBusConstants.QueueNames.MigrateFeedIndexId, Connection = "AzureConnectionString")] string item, TraceWriter log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnMigrateFeedIndexIdEvent.Run(message);

            log.Info($"C# Queue trigger function processed: {item}");
        }
    }
}
