using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Policy.ServiceBus
{
    public class OnReIndexTemplates : Retriable
    {
        private const string FunctionName = "on-policy-reindex-templates";
        private const string QueueName = ServiceBusConstants.QueueNames.PolicyReIndexTemplates;

        public OnReIndexTemplates(ILogger logger,
            ITemplatesReIndexerService templatesReIndexerService,
            IMessengerService messengerService,
             IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, templatesReIndexerService)
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
