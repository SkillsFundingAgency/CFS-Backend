using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Serilog;

namespace CalculateFunding.Functions.CosmosDbScaling.ServiceBus
{
    public static class OnScaleUpCosmosdbCollection
    {
        [FunctionName("on-scale-up-cosmosdb-collection")]
        public static async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.JobNotifications,
            ServiceBusConstants.TopicSubscribers.ScaleUpCosmosdbCollection,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            using (var scope = IocConfig.Build(config).CreateScope())
            {
                ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                IFeatureToggle featureToggle = scope.ServiceProvider.GetService<IFeatureToggle>();
                ILogger logger = scope.ServiceProvider.GetService<ILogger>();
                ICosmosDbScalingService scalingService = scope.ServiceProvider.GetService<ICosmosDbScalingService>();

                if (!featureToggle.IsCosmosDynamicScalingEnabled())
                {
                    return;
                }

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await scalingService.ScaleUp(message);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.TopicSubscribers.ScaleUpCosmosdbCollection}");
                    throw;
                }
            }
        }
    }
}
