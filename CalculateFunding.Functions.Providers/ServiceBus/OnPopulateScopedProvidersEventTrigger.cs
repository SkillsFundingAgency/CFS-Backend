using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
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
        public const string FunctionName = "on-populate-scopedproviders-event";

        public OnPopulateScopedProvidersEventTrigger(
            ILogger logger,
            IScopedProvidersService scopedProviderService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(scopedProviderService, nameof(scopedProviderService));

            _logger = logger;
            _scopedProviderService = scopedProviderService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.PopulateScopedProviders,
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
