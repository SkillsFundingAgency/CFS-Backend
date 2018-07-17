using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public static class OnCalcsInstructAllocationResults
    {
        [FunctionName("on-calcs-instruct-allocations")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CalculationJobInitialiser, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            using (var scope = IocConfig.Build(config).CreateScope())
            {
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var buildProjectsService = scope.ServiceProvider.GetService<IBuildProjectsService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await buildProjectsService.UpdateAllocations(message);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.CalculationJobInitialiser}");
                    throw;
                }

            }
        }
    }
}
