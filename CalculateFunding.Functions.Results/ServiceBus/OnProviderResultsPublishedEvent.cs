using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public static class OnProviderResultsPublishedEvent
    {
        [FunctionName("on-provider-results-published")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.PublishProviderResults, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            var config = ConfigHelper.AddConfig();

            using (var scope = IocConfig.Build(config).CreateScope())
            {
                var resultsService = scope.ServiceProvider.GetService<IResultsService>();
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await resultsService.PublishProviderResults(message);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.PublishProviderResults}");
                    throw;
                }

            }
        }
    }
}
