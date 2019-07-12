using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventHubs;
using CalculateFunding.Services.Core.Constants;
using Serilog;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Functions.CosmosDbScaling.EventHubs
{
    public class OnCosmosDbDiagnosticsReceived
    {
        private readonly ILogger _logger;
        private readonly ICosmosDbScalingService _scalingService;
        private readonly ICorrelationIdProvider _correlationIdProvider;

        public OnCosmosDbDiagnosticsReceived(
           ILogger logger,
           ICosmosDbScalingService scalingService,
           ICorrelationIdProvider correlationIdProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scalingService, nameof(scalingService));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));

            _logger = logger;
            _scalingService = scalingService;
            _correlationIdProvider = correlationIdProvider;
        }

        [FunctionName("OnCosmosDbDiagnosticsReceived")]
        public async Task Run([EventHubTrigger(EventHubsConstants.Hubs.CosmosDbDiagnostics, Connection = EventHubsConstants.ConnectionStringConfigurationKey)] EventData[] events)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(Guid.NewGuid().ToString());

                await _scalingService.ScaleUp(events);

                _logger.Information("Generate allocations complete");
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
