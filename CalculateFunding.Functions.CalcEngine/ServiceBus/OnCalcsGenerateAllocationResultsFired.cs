using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.CalcEngine.ServiceBus
{
    public static class OnCalcsGenerateAllocationResults
    {
        [FunctionName("on-calcs-generate-allocations-event")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var calculationEngineService = scope.ServiceProvider.GetService<ICalculationEngineService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

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
