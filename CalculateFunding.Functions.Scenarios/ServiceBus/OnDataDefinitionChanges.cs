using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public class OnDataDefinitionChanges : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IDatasetDefinitionFieldChangesProcessor _datasetDefinitionFieldChangesProcessor;
        public const string FunctionName = "on-data-definition-changes";

        public OnDataDefinitionChanges(
            ILogger logger,
            IDatasetDefinitionFieldChangesProcessor datasetDefinitionFieldChangesProcessor,
            IMessengerService messegerService,
            bool isDevelopment = false) : base(logger, messegerService, FunctionName, isDevelopment)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(datasetDefinitionFieldChangesProcessor, nameof(datasetDefinitionFieldChangesProcessor));

            _logger = logger;
            _datasetDefinitionFieldChangesProcessor = datasetDefinitionFieldChangesProcessor;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.DataDefinitionChanges,
            ServiceBusConstants.TopicSubscribers.UpdateScenarioFieldDefinitionProperties,
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
