using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnProviderResultsPublishedFailure
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IJobHelperService _jobHelperService;

        public OnProviderResultsPublishedFailure(
            ILogger logger,
            IJobHelperService jobHelperService,
            ICorrelationIdProvider correlationIdProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobHelperService, nameof(jobHelperService));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _jobHelperService = jobHelperService;
        }

        [FunctionName("on-provider-results-published-poisoned")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.PublishProviderResultsPoisoned, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _jobHelperService.ProcessDeadLetteredMessage(message);

                _logger.Information("Proccessed publish provider results dead lettered message complete");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.PublishProviderResultsPoisoned}");
                throw;
            }
        }
    }
}