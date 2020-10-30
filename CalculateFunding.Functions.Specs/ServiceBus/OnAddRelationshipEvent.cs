using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Specs.ServiceBus
{
    public class OnAddRelationshipEvent : Retriable
    {
        public const string FunctionName = "on-add-relationship-event";
        private const string QueueName = ServiceBusConstants.QueueNames.AddDefinitionRelationshipToSpecification;

        public OnAddRelationshipEvent(
            ILogger logger,
            ISpecificationsService specificationsService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, specificationsService)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(QueueName, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await base.Run(message);
        }
    }
}
