using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public static class OnDataDefinitionChanges
    {
        [FunctionName("on-data-definition-changes")]
        public static async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.DataDefinitionChanges, 
            ServiceBusConstants.TopicSubscribers.UpdateDataDefinitionName, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            using (var scope = IocConfig.Build(config).CreateScope())
            {
                ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                IDatasetDefinitionNameChangeProcessor datasetDefinitionChangesProcessor = scope.ServiceProvider.GetService<IDatasetDefinitionNameChangeProcessor>();
               
                ILogger logger = scope.ServiceProvider.GetService<ILogger>();
                correlationIdProvider.SetCorrelationId(message.GetCorrelationId());

                try
                {
                    await datasetDefinitionChangesProcessor.ProcessChanges(message);
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
