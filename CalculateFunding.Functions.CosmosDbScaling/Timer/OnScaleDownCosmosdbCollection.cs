using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.CosmosDbScaling.Timer
{
    public class OnScaleDownCosmosDbCollection
    {
        private const string Every15Minutes = "*/15 * * * *";

        private readonly ILogger _logger;
        private readonly ICosmosDbScalingService _scalingService;
        private readonly IConfigurationRefresher _configurationRefresher;

        public OnScaleDownCosmosDbCollection(
           ILogger logger,
           ICosmosDbScalingService scalingService,
           IConfigurationRefresherProvider refresherProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scalingService, nameof(scalingService));
            Guard.ArgumentNotNull(refresherProvider, nameof(refresherProvider));

            _logger = logger;
            _scalingService = scalingService;
            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName("on-scale-down-cosmosdb-collection")]
        public async Task Run([TimerTrigger(Every15Minutes)]TimerInfo timer)
        {
            try
            {
                await _configurationRefresher.TryRefreshAsync();

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
