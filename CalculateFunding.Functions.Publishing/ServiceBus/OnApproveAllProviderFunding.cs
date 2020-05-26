using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnApproveAllProviderFunding : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IApproveService _approveService;
        public const string FunctionName = FunctionConstants.PublishingApproveAllProviderFunding;
        public const string QueueName = ServiceBusConstants.QueueNames.PublishingApproveAllProviderFunding;

        public OnApproveAllProviderFunding(
            ILogger logger,
            IApproveService approveService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(approveService, nameof(approveService));

            _logger = logger;
            _approveService = approveService;
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
                    await _approveService.ApproveResults(message);
                }
                catch (NonRetriableException ex)
                {
                    _logger.Error(ex, $"Job threw non retriable exception: {QueueName}");
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {QueueName}");
                    throw;
                }
            },
            message);
        }
    }
}
