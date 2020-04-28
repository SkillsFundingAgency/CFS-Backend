using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.DeadletterProcessor;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnApproveAllProviderFundingFailure
    {
        private readonly ILogger _logger;
        private readonly IJobHelperService _jobHelperService;
        public const string FunctionName = FunctionConstants.PublishingApproveAllProviderFundingPoisoned;
        public const string QueueName = ServiceBusConstants.QueueNames.PublishingApproveAllProviderFundingPoisoned;

        public OnApproveAllProviderFundingFailure(
            ILogger logger,
            IJobHelperService jobHelperService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobHelperService, nameof(jobHelperService));

            _logger = logger;
            _jobHelperService = jobHelperService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            _logger.Information("Starting to process dead letter message for approving specification funding.");

            try
            {
                await _jobHelperService.ProcessDeadLetteredMessage(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {QueueName}");
                throw;
            }
        }
    }
}
