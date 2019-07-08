using System;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnProviderResultsPublishedEvent
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IPublishedResultsService _resultsService;

        public OnProviderResultsPublishedEvent(
            ILogger logger,
            IPublishedResultsService resultsService,
            ICorrelationIdProvider correlationIdProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _resultsService = resultsService;
        }

        [FunctionName("on-provider-results-published")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.PublishProviderResults, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());

                await _resultsService.PublishProviderResultsWithVariations(message);
            }
            catch (NonRetriableException ex)
            {
                _logger.Error(ex, $"A fatal error occurred while processing the message from the queue: {ServiceBusConstants.QueueNames.PublishProviderResults}");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred processing message from queue: {ServiceBusConstants.QueueNames.PublishProviderResults}");
                throw;
            }
        }
    }
}
