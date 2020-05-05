﻿using System.Threading.Tasks;
using CalculateFunding.Functions.Providers.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.DebugQueue
{
    public static class Providers
    {
        [FunctionName("on-populate-scopedproviders-event")]
        public static async Task RunOnPopulateScopedProvidersEventTrigger([QueueTrigger(ServiceBusConstants.QueueNames.PopulateScopedProviders, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Providers.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnPopulateScopedProvidersEventTrigger function = scope.ServiceProvider.GetService<OnPopulateScopedProvidersEventTrigger>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }

        [FunctionName("on-populate-scopedproviders-event-failure")]
        public static async Task RunOnPopulateScopedProvidersEventTriggerFailure([QueueTrigger(ServiceBusConstants.QueueNames.PopulateScopedProvidersPoisonedLocal, Connection = "AzureConnectionString")] string item, ILogger log)
        {
            using (IServiceScope scope = Functions.Providers.Startup.RegisterComponents(new ServiceCollection()).CreateScope())
            {
                Message message = Helpers.ConvertToMessage<string>(item);

                OnPopulateScopedProvidersEventTriggerFailure function = scope.ServiceProvider.GetService<OnPopulateScopedProvidersEventTriggerFailure>();

                await function.Run(message);

                log.LogInformation($"C# Queue trigger function processed: {item}");
            }
        }
    }
}
