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
    public class OnIncrementalScaleDownCosmosDbCollection
    {
        private const string Every15Minutes = "*/15 * * * *";

        private readonly ILogger _logger;
        private readonly ICosmosDbScalingService _scalingService;
        private readonly IConfigurationRefresher _configurationRefresher;

        public OnIncrementalScaleDownCosmosDbCollection(
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

        [FunctionName("on-incremental-scale-down-cosmosdb-collection")]
        public async Task Run([TimerTrigger(Every15Minutes)]TimerInfo timer)
        {
            try
            {
                await _configurationRefresher.TryRefreshAsync();

                await _scalingService.ScaleDownIncrementally();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "An error occurred getting message from timer job: on-incremental-scale-down-cosmosdb-collection");
                throw;
            }
        }
    }
}
