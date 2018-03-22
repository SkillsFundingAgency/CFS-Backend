using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.CalcEngine.EventHub
{
    public static class OnCalcsGenerateAllocationResults
    {
        [FunctionName("on-calcs-generate-allocations-event")]
        public static async Task Run([EventHubTrigger("calc-events-generate-allocations-results", Connection = "EventHubSettings:EventHubConnectionString")] EventData[] eventHubMessages)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var calculationEngineService = scope.ServiceProvider.GetService<ICalculationEngineService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                foreach (var message in eventHubMessages)
                {
                    try
                    {
                        correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                        await calculationEngineService.GenerateAllocations(message);
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
