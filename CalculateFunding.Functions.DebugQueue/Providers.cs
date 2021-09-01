using System.Threading.Tasks;
using CalculateFunding.Functions.Providers.ServiceBus;
using CalculateFunding.Functions.Providers.Timer;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Providers
    {
        private const string Every1Minute = "*/1 * * * *";

        [FunctionName("on-populate-scopedproviders-event")]
        public static async Task RunOnPopulateScopedProvidersEventTrigger([QueueTrigger(ServiceBusConstants.QueueNames.PopulateScopedProviders, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Providers.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnPopulateScopedProvidersEventTrigger function = scope.ServiceProvider.GetService<OnPopulateScopedProvidersEventTrigger>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName("on-populate-scopedproviders-event-failure")]
        public static async Task RunOnPopulateScopedProvidersEventTriggerFailure([QueueTrigger(ServiceBusConstants.QueueNames.PopulateScopedProvidersPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Providers.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnPopulateScopedProvidersEventTriggerFailure function = scope.ServiceProvider.GetService<OnPopulateScopedProvidersEventTriggerFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.ProviderSnapshotDataLoad)]
        public static async Task RunOnProviderSnapshotDataLoadEventTrigger([QueueTrigger(ServiceBusConstants.QueueNames.ProviderSnapshotDataLoad, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Providers.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnProviderSnapshotDataLoadEventTrigger function = scope.ServiceProvider.GetService<OnProviderSnapshotDataLoadEventTrigger>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.ProviderSnapshotDataLoadPoisoned)]
        public static async Task RunOnProviderSnapshotDataLoadEventTriggerFailure([QueueTrigger(ServiceBusConstants.QueueNames.ProviderSnapshotDataLoadPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Providers.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnProviderSnapshotDataLoadEventTriggerFailure function = scope.ServiceProvider.GetService<OnProviderSnapshotDataLoadEventTriggerFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.TrackLatest)]
        public static async Task RunOnTrackLatestEventTrigger([QueueTrigger(ServiceBusConstants.QueueNames.TrackLatest, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Providers.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnTrackLatestEventTrigger function = scope.ServiceProvider.GetService<OnTrackLatestEventTrigger>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.TrackLatestPoisoned)]
        public static async Task RunOnTrackLatestEventTriggerFailure([QueueTrigger(ServiceBusConstants.QueueNames.TrackLatestPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using IServiceScope scope = Functions.Providers.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            Message message = Helpers.ConvertToMessage<string>(item);

            OnTrackLatestEventTriggerFailure function = scope.ServiceProvider.GetService<OnTrackLatestEventTriggerFailure>();

            await function.Run(message);

            log.LogInformation($"C# Queue trigger function processed: {item}");
        }

        [FunctionName(FunctionConstants.NewProviderVersionCheck)]
        public static async Task RunOnNewProviderVersionCheck([TimerTrigger(Every1Minute, RunOnStartup = true)] TimerInfo timerInfo)
        {
            using IServiceScope scope = Functions.Providers.Startup.RegisterComponents(new ServiceCollection()).CreateScope();
            OnNewProviderVersionCheck function = scope.ServiceProvider.GetService<OnNewProviderVersionCheck>();

            await function.Run(timerInfo);
        }
    }
}
