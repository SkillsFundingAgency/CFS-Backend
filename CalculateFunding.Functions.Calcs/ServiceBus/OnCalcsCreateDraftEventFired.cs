using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public static class OnCalcsCreateDraftEvent
    {

        [FunctionName("on-calcs-create-draft-event")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CreateDraftCalculation, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var calculationService = scope.ServiceProvider.GetService<ICalculationService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();
                ICacheProvider cacheProvider = scope.ServiceProvider.GetService<ICacheProvider>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await calculationService.CreateCalculation(message);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.CreateDraftCalculation}");
                    throw;
                }
                
            }
        }

    }
}
