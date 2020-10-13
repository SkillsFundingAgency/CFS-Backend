using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Core;

namespace CalculateFunding.Functions.Providers.ServiceBus
{
    public class OnProviderSnapshotDataLoadEventTrigger : SmokeTest
    {
        private readonly ILogger _logger;
        private IProviderSnapshotDataLoadService _providerSnapshotDataLoadService;

        public const string FunctionName = FunctionConstants.ProviderSnapshotDataLoad;
        public const string QueueName = ServiceBusConstants.QueueNames.ProviderSnapshotDataLoad;

        public OnProviderSnapshotDataLoadEventTrigger(
            ILogger logger,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IProviderSnapshotDataLoadService providerSnapshotDataLoadService,
            bool useAzureStorage = false)
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(providerSnapshotDataLoadService, nameof(providerSnapshotDataLoadService));

            _logger = logger;
            _providerSnapshotDataLoadService = providerSnapshotDataLoadService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            await Run(async () =>
            {
                try
                {
                    await _providerSnapshotDataLoadService.LoadProviderSnapshotData(message);
                }
                catch (NonRetriableException ex)
                {
                    _logger.Error(ex, $"Job threw non retriable exception: {QueueName}");
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.ProviderSnapshotDataLoad}");
                    throw;
                }
            },
            message);
        }
    }
}
