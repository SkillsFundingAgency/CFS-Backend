using System;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Utility;
using CalculateFunding.Functions.CosmosDbScaling;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CalculateFunding.Functions.CosmosDbScaling.Timer
{

    public class OnScaleDownCosmosDbCollection
    {
        private readonly ILogger _logger;
        private readonly ICosmosDbScalingService _scalingService;
        private readonly IFeatureToggle _featureToggle;

        public OnScaleDownCosmosDbCollection(
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

        [FunctionName("on-scale-down-cosmosdb-collection")]
        public async Task Run([TimerTrigger("*/15 * * * *")]TimerInfo timer)
        {
            if (!_featureToggle.IsCosmosDynamicScalingEnabled())
            {
                return;
            }

            try
            {
                await _scalingService.ScaleDownForJobConfiguration();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "An error occurred getting message from timer job: on-scale-down-cosmosdb-collection");
                throw;
            }
        }
    }
}
