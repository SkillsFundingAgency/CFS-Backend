using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public class OnDataDefinitionChanges : Retriable
    {
        public const string FunctionName = "on-data-definition-changes";

        public OnDataDefinitionChanges(
            ILogger logger,
            IDatasetDefinitionFieldChangesProcessor datasetDefinitionFieldChangesProcessor,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, $"{ServiceBusConstants.TopicNames.DataDefinitionChanges}/{ServiceBusConstants.TopicSubscribers.UpdateScenarioFieldDefinitionProperties}", useAzureStorage, userProfileProvider, datasetDefinitionFieldChangesProcessor, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.DataDefinitionChanges,
            ServiceBusConstants.TopicSubscribers.UpdateScenarioFieldDefinitionProperties,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await base.Run(message);
        }
    }
}
