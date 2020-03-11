using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.DeadletterProcessor;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnGeneratePublishedProviderEstateCsvFailure
    {
        private readonly ILogger _logger;
        private readonly IJobHelperService _jobHelperService;

        public OnGeneratePublishedProviderEstateCsvFailure(
            ILogger logger,
            IJobHelperService jobHelperService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobHelperService, nameof(jobHelperService));

            _logger = logger;
            _jobHelperService = jobHelperService;
        }

        [FunctionName("on-publishing-generate-published-provider-estate-csv-poisoned")]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.GeneratePublishedProviderEstateCsvPoisoned,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message)
        {
            _logger.Information("Starting to process dead letter message for generate published provide estate csv.");

            try
            {
                await _jobHelperService.ProcessDeadLetteredMessage(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.GeneratePublishedProviderEstateCsvPoisoned}");

                throw;
            }
        }
    }
}
