using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Calcs.EventHub
{
    public static class OnCalcsInstructAllocationResults
    {
        public const string EventHubName = "calc-events-instruct-generate-allocations";

        [FunctionName("on-calcs-instruct-allocations")]
        public static async Task Run([EventHubTrigger(EventHubName, Connection = "EventHubSettings:EventHubConnectionString")] EventData[] eventHubMessages)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var buildProjectsService = scope.ServiceProvider.GetService<IBuildProjectsService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();
                ICacheProvider cacheProvider = scope.ServiceProvider.GetService<ICacheProvider>();

                foreach (var message in eventHubMessages)
                {
                    try
                    {
                        bool alreadyExists = await cacheProvider.HasMessageBeenProcessed(EventHubName, message);
                        if (!alreadyExists)
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                            await buildProjectsService.UpdateAllocations(message);
                            await cacheProvider.MarkMessageAsProcessed(EventHubName, message);
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.Error(exception, "An error occurred getting message from hub: calc-events-generate-allocations");
                        throw;
                    }
                }
            }
        }
    }
}
