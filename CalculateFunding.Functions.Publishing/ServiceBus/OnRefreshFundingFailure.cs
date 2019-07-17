using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnRefreshFundingFailure
    {
        private readonly ILogger _logger;
        private readonly IJobHelperService _jobHelperService;
        private readonly ICorrelationIdProvider _correlationIdProvider;

        public OnRefreshFundingFailure(
            ILogger logger,
            IJobHelperService jobHelperService,
            ICorrelationIdProvider correlationIdProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobHelperService, nameof(jobHelperService));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));

            _logger = logger;
            _jobHelperService = jobHelperService;
            _correlationIdProvider = correlationIdProvider;
        }

        [FunctionName("on-publishing-refresh-funding-poisoned")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.PublishingRefreshFundingPoisoned,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            _logger.Information("Starting to process dead letter message for refreshing funding.");

            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _jobHelperService.ProcessDeadLetteredMessage(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.PublishingRefreshFundingPoisoned}");
                throw;
            }
        }
    }
}
