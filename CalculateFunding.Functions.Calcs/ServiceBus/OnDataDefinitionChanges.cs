using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnDataDefinitionChanges : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IDatasetDefinitionFieldChangesProcessor _datasetDefinitionFieldChangesProcessor;
        public const string FunctionName = "on-data-definition-changes";

        public OnDataDefinitionChanges(
            ILogger logger,
            IDatasetDefinitionFieldChangesProcessor datasetDefinitionFieldChangesProcessor,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(datasetDefinitionFieldChangesProcessor, nameof(datasetDefinitionFieldChangesProcessor));

            _logger = logger;
            _datasetDefinitionFieldChangesProcessor = datasetDefinitionFieldChangesProcessor;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.DataDefinitionChanges,
            ServiceBusConstants.TopicSubscribers.UpdateCalculationFieldDefinitionProperties,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _datasetDefinitionFieldChangesProcessor.ProcessChanges(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.DataDefinitionChanges}");
                    throw;
                }
            },
            message);
        }
    }
}
