using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.CalcEngine.EventHub
{
    public static class OnCalcsGenerateAllocationResults
    {
        public const string EventHubName = "calc-events-generate-allocations-results";

        [FunctionName("on-calcs-generate-allocations-event")]
        public static async Task Run([EventHubTrigger(EventHubName, Connection = "EventHubSettings:EventHubConnectionString")] EventData[] eventHubMessages)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var calculationEngineService = scope.ServiceProvider.GetService<ICalculationEngineService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();
                ICacheProvider cacheProvider = scope.ServiceProvider.GetService<ICacheProvider>();

                foreach (EventData message in eventHubMessages)
                {
                    try
                    {
                        bool alreadyExists = await cacheProvider.HasMessageBeenProcessed(EventHubName, message);
                        if (!alreadyExists)
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                            await calculationEngineService.GenerateAllocations(message);
                            await cacheProvider.MarkMessageAsProcessed(EventHubName, message);
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.Error(exception, "An error occurred getting message from hub: calc-events-generate-allocations-results");
                        throw;
                    }
                }
            }
        }
    }
}
