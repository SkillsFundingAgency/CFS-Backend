using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnPublishFunding
    {
        private readonly ILogger _logger;
        private readonly IPublishService _publishService;

        public OnPublishFunding(
            ILogger logger,
            PublishService publishService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishService, nameof(publishService));

            _logger = logger;
            _publishService = publishService;
        }

        [FunctionName("on-publishing-publish-funding")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.PublishingPublishFunding,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            try
            {
                await _publishService.PublishResults(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.QueueNames.PublishingPublishFunding}");
                throw;
            }
        }
    }
}
