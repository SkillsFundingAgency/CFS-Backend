using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Calcs.EventHub
{
    public static class OnCalcsCreateDraftFired
    {
        [FunctionName("on-calcs-create-draft-fired")]
        public static async Task Run([EventHubTrigger("calc-events-create-draft", Connection = "EventHubSettings:EventHubConnectionString")] EventData[] eventHubMessages)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var calculationService = scope.ServiceProvider.GetService<ICalculationService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                foreach (var message in eventHubMessages)
                {
                    try
                    {
                        correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                        await calculationService.CreateCalculation(message);
                    }
                    catch (Exception exception)
                    {
                        logger.Error(exception, "An error occurred getting message from hub: calc-events-create-draft");
                        throw;
                    }
                }
            }
        }
    }
}
