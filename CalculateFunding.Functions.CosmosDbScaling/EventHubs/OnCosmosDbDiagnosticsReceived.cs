using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.CosmosDbScaling.EventHubs
{
    public class OnCosmosDbDiagnosticsReceived
    {

        private readonly ILogger _logger;
        private readonly ICosmosDbScalingService _scalingService;

        public OnCosmosDbDiagnosticsReceived(
           ILogger logger,
           ICosmosDbScalingService scalingService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scalingService, nameof(scalingService));

            _logger = logger;
            _scalingService = scalingService;
        }

        [FunctionName("OnCosmosDbDiagnosticsReceived")]
        public async Task Run([EventHubTrigger(EventHubsConstants.Hubs.CosmosDbDiagnostics, Connection = EventHubsConstants.ConnectionStringConfigurationKey)] EventData[] events)
        {
            try
            {
                await _scalingService.ScaleUp(events);
            }
            catch (NonRetriableException nrEx)
            {
                _logger.Error(nrEx, $"An error occurred processing messages on event hub: {EventHubsConstants.Hubs.CosmosDbDiagnostics}");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred processing messages on event hub: {EventHubsConstants.Hubs.CosmosDbDiagnostics}");
                throw;
            }
        }
    }
}
