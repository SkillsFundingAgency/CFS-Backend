using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public class OnEditCalculationEvent : SmokeTest
    {
        public const string FunctionName = "on-edit-calculation-for-scenarios";

        public OnEditCalculationEvent(
            ILogger logger,
            IScenariosService scenariosService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider, scenariosService)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditCalculation,
            ServiceBusConstants.TopicSubscribers.UpdateScenariosForEditCalculation,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await base.Run(message);
        }
    }
}
