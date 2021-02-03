using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnApproveBatchProviderFunding : Retriable
    {
        private readonly IApproveService _approveService;
        public const string FunctionName = FunctionConstants.PublishingApproveBatchProviderFunding;
        public const string QueueName = ServiceBusConstants.QueueNames.PublishingApproveBatchProviderFunding;

        public OnApproveBatchProviderFunding(
            ILogger logger,
            IApproveService approveService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, approveService, refresherProvider)
        {
            Guard.ArgumentNotNull(approveService, nameof(approveService));

            _approveService = approveService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await base.Run(message, async () =>
            {
                await _approveService.ApproveResults(message, batched: true);
            });
        }
    }
}
