using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public static class OnEditCalculationSpecificationEvent
    {
        [FunctionName("on-edit-calculation-for-calcs")]
        public static async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditCalculation,
            ServiceBusConstants.TopicSubscribers.UpdateCalculationsForEditCalculation,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var calculationService = scope.ServiceProvider.GetService<ICalculationService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await calculationService.UpdateCalculationsForCalculationSpecificationChange(message);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.EditCalculation}");
                    throw;
                }

            }
        }
    }
}
