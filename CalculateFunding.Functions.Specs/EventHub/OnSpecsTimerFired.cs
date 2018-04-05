using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Specs.EventHub
{
    public static class OnAddRelatioshipEvent
    {
        public const string EventHubName = "spec-events-add-definition-relationship";

        [FunctionName("on-add-relationship-event")]
        public static async Task Run([EventHubTrigger(EventHubName, Connection = "EventHubSettings:EventHubConnectionString")] EventData[] eventHubMessages)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var specificationsService = scope.ServiceProvider.GetService<ISpecificationsService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
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
                            await specificationsService.AssignDataDefinitionRelationship(message);
                            await cacheProvider.MarkMessageAsProcessed(EventHubName, message);
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.Error(exception, "An error occurred getting message from hub: spec-events-add-definition-relationship");
                        throw;
                    }
                }
            }
        }
    }
}
