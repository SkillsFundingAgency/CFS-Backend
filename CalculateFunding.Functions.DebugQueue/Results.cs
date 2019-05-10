using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Messages;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Results
    {
        [FunctionName("on-provider-results-published")]
        public static async Task RunPublishProviderResults([QueueTrigger(ServiceBusConstants.QueueNames.PublishProviderResults, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnProviderResultsPublishedEvent.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-provider-results-published-poisoned")]
        public static async Task RunPublishProviderResultsPoisoned([QueueTrigger(ServiceBusConstants.QueueNames.PublishProviderResultsPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<IEnumerable<FetchProviderProfilingMessageItem>>(item);

            await Functions.Results.ServiceBus.OnProviderResultsPublishedFailure.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-fetch-provider-profile")]
        public static async Task RunFetchProviderProfile([QueueTrigger(ServiceBusConstants.QueueNames.FetchProviderProfile, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<IEnumerable<FetchProviderProfilingMessageItem>>(item);

            await Functions.Results.ServiceBus.OnFetchProviderProfileEvent.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-fetch-provider-profile-poisoned")]
        public static async Task RunFetchProviderProfilePoisoned([QueueTrigger(ServiceBusConstants.QueueNames.FetchProviderProfilePoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<IEnumerable<FetchProviderProfilingMessageItem>>(item);

            await Functions.Results.ServiceBus.OnFetchProviderProfileFailure.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-migrate-result-versions")]
        public static async Task RunMigrateResultVersions([QueueTrigger(ServiceBusConstants.QueueNames.MigrateResultVersions, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnMigrateResultVersionsEvent.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-migrate-feed-index-id")]
        public static async Task RunMigrateResultFeedIndexes([QueueTrigger(ServiceBusConstants.QueueNames.MigrateFeedIndexId, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnMigrateFeedIndexIdEvent.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-allocationline-result-status-updates")]
        public static async Task RunAllocationLineResultStatusUpdates([QueueTrigger(ServiceBusConstants.QueueNames.AllocationLineResultStatusUpdates, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<UpdatePublishedAllocationLineResultStatusModel>(item);

            await Functions.Results.ServiceBus.OnCreateAllocationLineResultStatusUpdates.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-instruct-allocationline-result-status-updates")]
        public static async Task RunInstructAllocationLineResultStatusUpdates([QueueTrigger(ServiceBusConstants.QueueNames.InstructAllocationLineResultStatusUpdates, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnCreateInstructAllocationLineResultStatusUpdates.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-allocationline-result-status-updates-poisoned")]
        public static async Task RunAllocationLineResultStatusUpdatesFailure([QueueTrigger(ServiceBusConstants.QueueNames.AllocationLineResultStatusUpdatesPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<UpdatePublishedAllocationLineResultStatusModel>(item);

            await Functions.Results.ServiceBus.OnCreateAllocationLineResultStatusUpdatesFailure.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-instruct-allocationline-result-status-updates-poisoned")]
        public static async Task RunInstructAllocationLineResultStatusUpdatesFailure([QueueTrigger(ServiceBusConstants.QueueNames.InstructAllocationLineResultStatusUpdatesPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnCreateInstructAllocationLineResultStatusUpdatesFailure.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-reindex-allocation-notification-feeds")]
        public static async Task RunReIndexAllocationNotificationFeeds([QueueTrigger(ServiceBusConstants.QueueNames.ReIndexAllocationNotificationFeedIndex, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnReIndexAllocationNotificationFeeds.Run(message);

            log.LogInformation($"C# Queue trigger function processed for {ServiceBusConstants.QueueNames.ReIndexAllocationNotificationFeedIndex}: {item}");
        }

        [FunctionName("on-reindex-calculation-results")]
        public static async Task RunReIndexCalculationResults([QueueTrigger(ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            Message message = Helpers.ConvertToMessage<string>(item);

            await Functions.Results.ServiceBus.OnReIndexCalculationResults.Run(message);

            log.LogInformation($"C# Queue trigger function processed {ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex}: {item}");
        }
    }
}
