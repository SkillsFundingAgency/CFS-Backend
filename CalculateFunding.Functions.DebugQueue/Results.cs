using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Functions.Results.ServiceBus;
using CalculateFunding.Functions.Results.Timer;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Messages;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Results
    {
        [FunctionName("on-provider-results-published")]
        public static async Task RunPublishProviderResults([QueueTrigger(ServiceBusConstants.QueueNames.PublishProviderResults, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnProviderResultsPublishedEvent function = scope.ServiceProvider.GetService<OnProviderResultsPublishedEvent>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-provider-results-published-poisoned")]
        public static async Task RunPublishProviderResultsPoisoned([QueueTrigger(ServiceBusConstants.QueueNames.PublishProviderResultsPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<IEnumerable<FetchProviderProfilingMessageItem>>(item);

                OnProviderResultsPublishedFailure function = scope.ServiceProvider.GetService<OnProviderResultsPublishedFailure>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-fetch-provider-profile")]
        public static async Task RunFetchProviderProfile([QueueTrigger(ServiceBusConstants.QueueNames.FetchProviderProfile, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<IEnumerable<FetchProviderProfilingMessageItem>>(item);

                OnFetchProviderProfileEvent function = scope.ServiceProvider.GetService<OnFetchProviderProfileEvent>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-fetch-provider-profile-poisoned")]
        public static async Task RunFetchProviderProfilePoisoned([QueueTrigger(ServiceBusConstants.QueueNames.FetchProviderProfilePoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<IEnumerable<FetchProviderProfilingMessageItem>>(item);

                OnFetchProviderProfileFailure function = scope.ServiceProvider.GetService<OnFetchProviderProfileFailure>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-migrate-result-versions")]
        public static async Task RunMigrateResultVersions([QueueTrigger(ServiceBusConstants.QueueNames.MigrateResultVersions, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnMigrateResultVersionsEvent function = scope.ServiceProvider.GetService<OnMigrateResultVersionsEvent>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-migrate-feed-index-id")]
        public static async Task RunMigrateResultFeedIndexes([QueueTrigger(ServiceBusConstants.QueueNames.MigrateFeedIndexId, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnMigrateFeedIndexIdEvent function = scope.ServiceProvider.GetService<OnMigrateFeedIndexIdEvent>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-allocationline-result-status-updates")]
        public static async Task RunAllocationLineResultStatusUpdates([QueueTrigger(ServiceBusConstants.QueueNames.AllocationLineResultStatusUpdates, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<UpdatePublishedAllocationLineResultStatusModel>(item);

                OnCreateAllocationLineResultStatusUpdates function = scope.ServiceProvider.GetService<OnCreateAllocationLineResultStatusUpdates>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-instruct-allocationline-result-status-updates")]
        public static async Task RunInstructAllocationLineResultStatusUpdates([QueueTrigger(ServiceBusConstants.QueueNames.InstructAllocationLineResultStatusUpdates, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnCreateInstructAllocationLineResultStatusUpdates function = scope.ServiceProvider.GetService<OnCreateInstructAllocationLineResultStatusUpdates>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-allocationline-result-status-updates-poisoned")]
        public static async Task RunAllocationLineResultStatusUpdatesFailure([QueueTrigger(ServiceBusConstants.QueueNames.AllocationLineResultStatusUpdatesPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<UpdatePublishedAllocationLineResultStatusModel>(item);

                OnCreateInstructAllocationLineResultStatusUpdates function = scope.ServiceProvider.GetService<OnCreateInstructAllocationLineResultStatusUpdates>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-instruct-allocationline-result-status-updates-poisoned")]
        public static async Task RunInstructAllocationLineResultStatusUpdatesFailure([QueueTrigger(ServiceBusConstants.QueueNames.InstructAllocationLineResultStatusUpdatesPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnCreateInstructAllocationLineResultStatusUpdatesFailure function = scope.ServiceProvider.GetService<OnCreateInstructAllocationLineResultStatusUpdatesFailure>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-reindex-allocation-notification-feeds")]
        public static async Task RunReIndexAllocationNotificationFeeds([QueueTrigger(ServiceBusConstants.QueueNames.ReIndexAllocationNotificationFeedIndex, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnReIndexAllocationNotificationFeeds function = scope.ServiceProvider.GetService<OnReIndexAllocationNotificationFeeds>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-reindex-calculation-results")]
        public static async Task RunReIndexCalculationResults([QueueTrigger(ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnReIndexCalculationResults function = scope.ServiceProvider.GetService<OnReIndexCalculationResults>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-calculation-results-csv-generation")]
        public static async Task RunCalculationResultsCsvGeneration([QueueTrigger(ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnCalculationResultsCsvGeneration function = scope.ServiceProvider.GetService<OnCalculationResultsCsvGeneration>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-calculation-results-csv-generation-timer")]
        public static async Task RunCalculationResultsCsvGenerationTimer([QueueTrigger(ServiceBusConstants.QueueNames.CalculationResultsCsvGenerationTimer, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Results.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                TimerInfo timerInfo = new TimerInfo(null, new ScheduleStatus());

                OnCalculationResultsCsvGenerationTimer function = scope.ServiceProvider.GetService<OnCalculationResultsCsvGenerationTimer>();

                await function.Run(timerInfo);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
