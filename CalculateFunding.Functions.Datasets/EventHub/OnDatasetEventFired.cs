using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Datasets.EventHub
{
    public static class OnDatasetEventFired
    {
        [FunctionName("on-dataset-event-fired")]
        public static async Task Run([EventHubTrigger("dataset-events-datasets", Connection = "EventHubSettings:EventHubConnectionString")] EventData[] eventHubMessages)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var datasetService = scope.ServiceProvider.GetService<IDatasetService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                foreach (var message in eventHubMessages)
                {
                    try
                    {
                        correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                        await datasetService.ProcessDataset(message);
                    }
                    catch (Exception exception)
                    {
                        logger.Error(exception, "An error occurred getting message from hub: dataset-events-datasets");
                        throw;
                    }
                }
            }
        }
    }
}
