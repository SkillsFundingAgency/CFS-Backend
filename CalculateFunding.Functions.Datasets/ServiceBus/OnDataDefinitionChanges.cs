using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnDataDefinitionChanges : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IDatasetDefinitionNameChangeProcessor _datasetDefinitionChangesProcessor;
        public const string FunctionName = "on-data-definition-changes";

        public OnDataDefinitionChanges(
            ILogger logger,
            IDatasetDefinitionNameChangeProcessor datasetDefinitionChangesProcessor,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(datasetDefinitionChangesProcessor, nameof(datasetDefinitionChangesProcessor));

            _logger = logger;
            _datasetDefinitionChangesProcessor = datasetDefinitionChangesProcessor;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.DataDefinitionChanges,
            ServiceBusConstants.TopicSubscribers.UpdateDataDefinitionName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _datasetDefinitionChangesProcessor.ProcessChanges(message);
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
