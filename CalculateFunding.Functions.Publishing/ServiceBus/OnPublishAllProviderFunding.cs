using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnPublishAllProviderFunding : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IPublishService _publishService;
        public const string FunctionName = FunctionConstants.PublishingPublishAllProviderFunding;
        public const string QueueName = ServiceBusConstants.QueueNames.PublishingPublishAllProviderFunding;

        public OnPublishAllProviderFunding(
            ILogger logger,
            IPublishService publishService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishService, nameof(publishService));

            _logger = logger;
            _publishService = publishService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _publishService.PublishProviderFundingResults(message);
                }
                catch (NonRetriableException ex)
                {
                    _logger.Error(ex, $"Job threw non retriable exception: {QueueName}");
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from topic: {QueueName}");
                    throw;
                }
            },
            message);
        }
    }
}
