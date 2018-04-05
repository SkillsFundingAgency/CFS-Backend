using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Datasets.EventHub
{
    public static class OnDatasetEvent
    {
        public const string EventHubName = "dataset-events-datasets";

        [FunctionName("on-dataset-event")]
        public static async Task Run([EventHubTrigger(EventHubName, Connection = "EventHubSettings:EventHubConnectionString")] EventData[] eventHubMessages)
        {

            using (var scope = IocConfig.Build().CreateScope())
            {
                var datasetService = scope.ServiceProvider.GetService<IDatasetService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();
                ICacheProvider cacheProvider = scope.ServiceProvider.GetService<ICacheProvider>();

                foreach (var message in eventHubMessages)
                {
                    try
                    {
                        bool alreadyExists = await cacheProvider.HasMessageBeenProcessed(EventHubName, message);
                        if (!alreadyExists)
                        {
                            correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                            await datasetService.ProcessDataset(message);
                            await cacheProvider.MarkMessageAsProcessed(EventHubName, message);
                        }
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
