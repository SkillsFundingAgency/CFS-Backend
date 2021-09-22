using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnReleaseProvidersToChannels : Retriable
    {
        public const string FunctionName = FunctionConstants.PublishingReleaseProvidersToChannels;
        public const string QueueName = ServiceBusConstants.QueueNames.PublishingReleaseProvidersToChannels;

        public OnReleaseProvidersToChannels(
            ILogger logger,
            IReleaseProvidersToChannelsService releaseProvidersToChannelsService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false)
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, releaseProvidersToChannelsService, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message) => await base.Run(message);
    }
}
