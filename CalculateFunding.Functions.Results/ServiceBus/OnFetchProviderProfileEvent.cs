using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnFetchProviderProfileEvent
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IPublishedResultsService _resultsService;

        public OnFetchProviderProfileEvent(
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

        [FunctionName("on-fetch-provider-profile")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.FetchProviderProfile, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _resultsService.FetchProviderProfile(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.FetchProviderProfile}");
                throw;
            }
        }
    }
}
