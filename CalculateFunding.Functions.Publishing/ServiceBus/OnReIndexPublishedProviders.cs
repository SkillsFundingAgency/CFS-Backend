using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Publishing;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnReIndexPublishedProviders : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IPublishedProviderReIndexerService _publishedProviderReIndexerService;
        public const string FunctionName = "on-publishing-reindex-published-providers";

        public OnReIndexPublishedProviders(
            ILogger logger,
            IPublishedProviderReIndexerService publishedProviderReIndexerService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishedProviderReIndexerService, nameof(publishedProviderReIndexerService));

            _logger = logger;
            _publishedProviderReIndexerService = publishedProviderReIndexerService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.PublishingReIndexPublishedProviders,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message)
        {
            await Run(async () =>
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
            },
            message);
        }
    }
}