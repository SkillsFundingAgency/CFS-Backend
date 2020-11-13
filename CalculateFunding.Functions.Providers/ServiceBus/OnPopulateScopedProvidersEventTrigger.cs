using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Providers.ServiceBus
{
    public class OnPopulateScopedProvidersEventTrigger : Retriable
    {
        private const string FunctionName = FunctionConstants.PopulateScopedProviders;
        private const string QueueName = ServiceBusConstants.QueueNames.PopulateScopedProviders;

        public OnPopulateScopedProvidersEventTrigger(
            ILogger logger,
            IScopedProvidersService scopedProviderService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, scopedProviderService)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        { 
            await base.Run(message);
        }
    }
}
