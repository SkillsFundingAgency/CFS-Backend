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
using CalculateFunding.Services.Processing.Functions;

namespace CalculateFunding.Functions.Providers.ServiceBus
{
    public class OnProviderSnapshotDataLoadEventTrigger : Retriable
    {
        public const string FunctionName = FunctionConstants.ProviderSnapshotDataLoad;
        public const string QueueName = ServiceBusConstants.QueueNames.ProviderSnapshotDataLoad;

        public OnProviderSnapshotDataLoadEventTrigger(
            ILogger logger,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IProviderSnapshotDataLoadService providerSnapshotDataLoadService,
            bool useAzureStorage = false)
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, providerSnapshotDataLoadService)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await base.Run(message);
        }
    }
}
