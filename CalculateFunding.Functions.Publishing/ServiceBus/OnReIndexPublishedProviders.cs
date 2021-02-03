using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Publishing;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnReIndexPublishedProviders : Retriable
    {
        public const string FunctionName = "on-publishing-reindex-published-providers";
        private const string QueueName = ServiceBusConstants.QueueNames.PublishingReIndexPublishedProviders;

        public OnReIndexPublishedProviders(
            ILogger logger,
            IPublishedProviderReIndexerService publishedProviderReIndexerService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, publishedProviderReIndexerService, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                QueueName,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message)
        {
            await base.Run(message);
        }
    }
}