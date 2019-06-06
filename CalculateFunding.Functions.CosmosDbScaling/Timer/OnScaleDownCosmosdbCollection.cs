using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CalculateFunding.Functions.CosmosDbScaling;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;

namespace CalculateFunding.Functions.CosmosDbScaling.Timer
{

    public static class OnScaleDownCosmosdbCollection
    {
        [FunctionName("on-scale-down-cosmosdb-collection")]
        public static async Task Run([TimerTrigger("*/15 * * * *")]TimerInfo timer)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            using (var scope = IocConfig.Build(config).CreateScope())
            {
                ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                IFeatureToggle featureToggle = scope.ServiceProvider.GetService<IFeatureToggle>();
                Serilog.ILogger logger = scope.ServiceProvider.GetService<Serilog.ILogger>();
                ICosmosDbScalingService scalingService = scope.ServiceProvider.GetService<ICosmosDbScalingService>();

                if (!featureToggle.IsCosmosDynamicScalingEnabled())
                {
                    return;
                }

                try
                {
                    await scalingService.ScaleDown();
                }
                catch (Exception exception)
                {
                    logger.Error(exception, "An error occurred getting message from timer job: on-scale-down-cosmosdb-collection");
                    throw;
                }
            }
        }
    }
}
