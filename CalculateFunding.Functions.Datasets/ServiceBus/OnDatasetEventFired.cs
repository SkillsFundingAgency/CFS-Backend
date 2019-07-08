using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnDatasetEvent
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IProcessDatasetService _processDatasetService;

        public OnDatasetEvent(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            IProcessDatasetService processDatasetService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(processDatasetService, nameof(processDatasetService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _processDatasetService = processDatasetService;
        }

        [FunctionName("on-dataset-event")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.ProcessDataset, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _processDatasetService.ProcessDataset(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.ProcessDataset}");
                throw;
            }
        }
    }
}
