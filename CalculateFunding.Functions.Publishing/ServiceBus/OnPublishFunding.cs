using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
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
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IPublishService _publishService;

        public OnPublishFunding(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            PublishService publishService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(publishService, nameof(publishService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _publishService = publishService;
        }

        [FunctionName("on-publishing-publish-funding")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.PublishingPublishFunding,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());

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
