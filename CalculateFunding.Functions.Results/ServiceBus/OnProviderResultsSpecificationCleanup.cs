using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Serilog;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnProviderResultsSpecificationCleanup
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IResultsService _resultsService;

        public OnProviderResultsSpecificationCleanup(
            ILogger logger,
            IResultsService resultsService,
            ICorrelationIdProvider correlationIdProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _resultsService = resultsService;
        }

        [FunctionName("on-provider-results-specification-cleanup")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup,
            ServiceBusConstants.TopicSubscribers.CleanupCalculationResultsForSpecificationProviders,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _resultsService.CleanupProviderResultsForSpecification(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup}");
                throw;
            }
        }
    }
}
