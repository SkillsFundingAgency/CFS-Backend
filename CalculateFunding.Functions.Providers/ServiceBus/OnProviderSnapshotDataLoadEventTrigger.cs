using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Providers;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

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
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            await Run(async () =>
            {
                try
                {
                    await _providerSnapshotDataLoadService.LoadProviderSnapshotData(message);
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
