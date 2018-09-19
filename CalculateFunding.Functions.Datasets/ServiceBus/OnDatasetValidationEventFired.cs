using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public static class OnDatasetValidationEvent
    {
        [FunctionName("on-dataset-validation-event")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.ValidateDataset, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            var config = ConfigHelper.AddConfig();

            using (var scope = IocConfig.Build(config).CreateScope())
            {
                var datasetService = scope.ServiceProvider.GetService<IDatasetService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await datasetService.ValidateDataset(message);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.ValidateDataset}");
                    throw;
                }

            }
        }
    }
}
