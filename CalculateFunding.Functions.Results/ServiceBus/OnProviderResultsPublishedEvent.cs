using System;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.Core;
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
            Microsoft.Extensions.Configuration.IConfigurationRoot config = ConfigHelper.AddConfig();

            using (IServiceScope scope = IocConfig.Build(config).CreateScope())
            {
                IPublishedResultsService resultsService = scope.ServiceProvider.GetService<IPublishedResultsService>();
                ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                Serilog.ILogger logger = scope.ServiceProvider.GetService<Serilog.ILogger>();
                IFeatureToggle featureToggle = scope.ServiceProvider.GetService<IFeatureToggle>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());

                    await resultsService.PublishProviderResultsWithVariations(message);
                }
                catch (NonRetriableException ex)
                {
                    logger.Error(ex, $"A fatal error occurred while processing the message from the queue: {ServiceBusConstants.QueueNames.PublishProviderResults}");
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred processing message from queue: {ServiceBusConstants.QueueNames.PublishProviderResults}");
                    throw;
                }
            }
        }
    }
}
