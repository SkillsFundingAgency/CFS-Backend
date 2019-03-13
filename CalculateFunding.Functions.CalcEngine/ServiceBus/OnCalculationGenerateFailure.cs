using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.CalcEngine.ServiceBus
{
    public static class OnCalculationGenerateFailure
    {
        /// <summary>
        /// On poisoned message for running calcs
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        [FunctionName("on-calcs-generate-allocations-event-poisoned")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResultsPoisoned, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]Message message)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            using (IServiceScope scope = IocConfig.Build(config).CreateScope())
            {
                ILogger logger = scope.ServiceProvider.GetService<ILogger>();
                logger.Information("Scope created, starting to process dead letter message for generating allocations");
                ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                IJobHelperService jobHelperService = scope.ServiceProvider.GetService<IJobHelperService>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await jobHelperService.ProcessDeadLetteredMessage(message);

                    logger.Information("Proccessed generate allocations dead lettered message complete");
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred processing message on queue: {ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResultsPoisoned}");
                    throw;
                }
            }
        }
    }
}
