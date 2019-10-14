using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnReIndexPublishedProviders
    {
        private readonly ILogger _logger;
        private readonly IPublishedProviderReIndexerService _publishedProviderReIndexerService;

        public OnReIndexPublishedProviders(
            ILogger logger,
            IPublishedProviderReIndexerService publishedProviderReIndexerService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishedProviderReIndexerService, nameof(publishedProviderReIndexerService));

            _logger = logger;
            _publishedProviderReIndexerService = publishedProviderReIndexerService;
        }

        [FunctionName("on-publishing-reindex-published-providers")]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.PublishingReIndexPublishedProviders,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
                IsSessionsEnabled = true)]
            Message message)
        {
            try
            {
                await _publishedProviderReIndexerService.Run(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.PublishingReIndexPublishedProviders}");

                throw;
            }
        }
    }
}