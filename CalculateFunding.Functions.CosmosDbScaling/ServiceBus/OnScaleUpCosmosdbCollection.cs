using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.CosmosDbScaling.ServiceBus
{
    public class OnScaleUpCosmosDbCollection
    {
        private readonly ILogger _logger;
        private readonly ICosmosDbScalingService _scalingService;
        private readonly IFeatureToggle _featureToggle;

        public OnScaleUpCosmosDbCollection(
           ILogger logger,
           ICosmosDbScalingService scalingService,
           IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scalingService, nameof(scalingService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _logger = logger;
            _scalingService = scalingService;
            _featureToggle = featureToggle;
        }

        [FunctionName("on-scale-up-cosmosdb-collection")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.JobNotifications,
            ServiceBusConstants.TopicSubscribers.ScaleUpCosmosdbCollection,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {

            if (_featureToggle.IsCosmosDynamicScalingEnabled())
            {
                try
                {
                    await _scalingService.ScaleUp(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.TopicSubscribers.ScaleUpCosmosdbCollection}");
                    throw;
                }
            }
        }
    }
}
