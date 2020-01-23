using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnDeleteDatasets
    {
        private readonly ILogger _logger;
        private readonly IDatasetService _datasetService;

        public OnDeleteDatasets(
            ILogger logger,
            IDatasetService datasetService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));

            _logger = logger;
            _datasetService = datasetService;
        }

        [FunctionName("on-delete-datasets")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeleteDatasets,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                await _datasetService.DeleteDatasets(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.DeleteDatasets}");
                throw;
            }
        }
    }
}
