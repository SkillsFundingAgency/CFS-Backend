using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.CosmosDbScaling.Timer
{
    public class OnIncrementalScaleDownCosmosDbCollection
    {
        private const string Every15Minutes = "*/15 * * * *";

        private readonly ILogger _logger;
        private readonly ICosmosDbScalingService _scalingService;

        public OnIncrementalScaleDownCosmosDbCollection(
           ILogger logger,
           ICosmosDbScalingService scalingService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scalingService, nameof(scalingService));

            _logger = logger;
            _scalingService = scalingService;
        }

        [FunctionName("on-incremental-scale-down-cosmosdb-collection")]
        public async Task Run([TimerTrigger(Every15Minutes)]TimerInfo timer)
        {
            try
            {
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
