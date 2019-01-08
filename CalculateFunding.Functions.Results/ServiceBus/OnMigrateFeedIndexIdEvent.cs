using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public static class OnMigrateFeedIndexIdEvent
    {
        [FunctionName("on-migrate-feed-index-id")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.MigrateFeedIndexId, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            var config = ConfigHelper.AddConfig();

            using (var scope = IocConfig.Build(config).CreateScope())
            {
                IPublishedResultsService resultsService = scope.ServiceProvider.GetService<IPublishedResultsService>();
                ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                Serilog.ILogger logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await resultsService.MigrateFeedIndexId(message);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.MigrateFeedIndexId}");
                    throw;
                }
            }
        }
    }
}
