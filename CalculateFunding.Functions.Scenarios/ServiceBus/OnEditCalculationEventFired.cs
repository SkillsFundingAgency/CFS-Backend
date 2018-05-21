using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public static class OnEditCaluclationEvent
    {
        [FunctionName("on-edit-calculation-for-scenarios")]
        public static async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditCalculation,
            ServiceBusConstants.TopicSubscribers.UpdateScenariosForEditCalculation,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var scenariosService = scope.ServiceProvider.GetService<IScenariosService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await scenariosService.UpdateScenarioForCalculation(message);
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
