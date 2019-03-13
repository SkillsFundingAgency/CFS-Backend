using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CalculateFunding.Functions.CalcEngine.ServiceBus
{
    public static class OnCalcsGenerateAllocationResults
    {
        [FunctionName("on-calcs-generate-allocations-event")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            using (IServiceScope scope = IocConfig.Build(config).CreateScope())
            {
                ILogger logger = scope.ServiceProvider.GetService<ILogger>();
                logger.Information("Scope created, starting to generate allocations");
                ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                ICalculationEngineService calculationEngineService = scope.ServiceProvider.GetService<ICalculationEngineService>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await calculationEngineService.GenerateAllocations(message);

                    logger.Information("Generate allocations complete");
                }
                catch (NonRetriableException nrEx)
                {
                    logger.Error(nrEx, $"An error occurred processing message on queue: {ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults}");
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred processing message on queue: {ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults}");
                    throw;
                }
            }
        }
    }
}
