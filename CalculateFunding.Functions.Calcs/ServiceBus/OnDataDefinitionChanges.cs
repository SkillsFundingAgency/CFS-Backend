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
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public static class OnDataDefinitionChanges
    {
        [FunctionName("on-data-definition-changes")]
        public static async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.DataDefinitionChanges,
            ServiceBusConstants.TopicSubscribers.UpdateCalculationFieldDefinitionProperties,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            using (var scope = IocConfig.Build(config).CreateScope())
            {
                ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                IDatasetDefinitionFieldChangesProcessor datasetDefinitionFieldChangesProcessor = scope.ServiceProvider.GetService<IDatasetDefinitionFieldChangesProcessor>();

                ILogger logger = scope.ServiceProvider.GetService<ILogger>();
                correlationIdProvider.SetCorrelationId(message.GetCorrelationId());

                try
                {
                    await datasetDefinitionFieldChangesProcessor.ProcessChanges(message);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.DataDefinitionChanges}");
                    throw;
                }
            }
        }
    }
}
