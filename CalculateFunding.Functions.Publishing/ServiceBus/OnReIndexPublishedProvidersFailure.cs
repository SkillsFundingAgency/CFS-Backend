using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnReIndexPublishedProvidersFailure
    {
        private readonly ILogger _logger;
        private readonly IJobHelperService _jobHelperService;

        public OnReIndexPublishedProvidersFailure(
            ILogger logger,
            IJobHelperService jobHelperService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobHelperService, nameof(jobHelperService));

            _logger = logger;
            _jobHelperService = jobHelperService;
        }

        [FunctionName("on-publishing-reindex-published-providers-poisoned")]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.PublishingReIndexPublishedProvidersPoisoned,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message)
        {
            _logger.Information("Starting to process dead letter message for reindex published providers.");

            try
            {
                await _jobHelperService.ProcessDeadLetteredMessage(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.PublishingReIndexPublishedProvidersPoisoned}");

                throw;
            }
        }
    }
}