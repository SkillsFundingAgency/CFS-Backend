using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnDataDefinitionChanges
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDatasetDefinitionNameChangeProcessor _datasetDefinitionChangesProcessor;

        public OnDataDefinitionChanges(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            IDatasetDefinitionNameChangeProcessor datasetDefinitionChangesProcessor)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(datasetDefinitionChangesProcessor, nameof(datasetDefinitionChangesProcessor));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _datasetDefinitionChangesProcessor = datasetDefinitionChangesProcessor;
        }

        [FunctionName("on-data-definition-changes")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.DataDefinitionChanges, 
            ServiceBusConstants.TopicSubscribers.UpdateDataDefinitionName, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());

            try
            {
                await _datasetDefinitionChangesProcessor.ProcessChanges(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.DataDefinitionChanges}");
                throw;
            }
        }
    }
}
