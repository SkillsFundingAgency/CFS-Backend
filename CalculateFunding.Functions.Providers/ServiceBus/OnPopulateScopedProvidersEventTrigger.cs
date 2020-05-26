using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Providers.ServiceBus
{
    public class OnPopulateScopedProvidersEventTrigger : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IScopedProvidersService _scopedProviderService;
        public const string FunctionName = FunctionConstants.PopulateScopedProviders;
        public const string QueueName = ServiceBusConstants.QueueNames.PopulateScopedProviders;

        public OnPopulateScopedProvidersEventTrigger(
            ILogger logger,
            IScopedProvidersService scopedProviderService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scopedProviderService, nameof(scopedProviderService));

            _logger = logger;
            _scopedProviderService = scopedProviderService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        { 
            Guard.ArgumentNotNull(message, nameof(message));

            await Run(async () =>
            {
                try
                {
                    await _scopedProviderService.PopulateScopedProviders(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.PopulateScopedProviders}");
                    throw;
                }
            },
            message);
        }
    }
}
