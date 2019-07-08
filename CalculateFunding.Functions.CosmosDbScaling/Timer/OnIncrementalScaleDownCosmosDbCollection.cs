using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Common.Utility;
using Serilog;

namespace CalculateFunding.Functions.CosmosDbScaling.Timer
{
    public class OnIncrementa1ScaleDownCosmosDbCollection
    {
        private readonly ILogger _logger;
        private readonly ICosmosDbScalingService _scalingService;
        private readonly IFeatureToggle _featureToggle;

        public OnIncrementa1ScaleDownCosmosDbCollection(
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

        [FunctionName("on-incremental-scale-down-cosmosdb-collection")]
        public async Task Run([TimerTrigger("*/15 * * * *")]TimerInfo timer)
        {
            if (!_featureToggle.IsCosmosDynamicScalingEnabled())
            {
                return;
            }

            try
            {
                await _scalingService.ScaleDowmIncrementally();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "An error occurred getting message from timer job: on-incremental-scale-down-cosmosdb-collection");
                throw;
            }
        }
    }
}
