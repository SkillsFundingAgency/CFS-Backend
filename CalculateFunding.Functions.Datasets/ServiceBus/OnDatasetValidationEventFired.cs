using System;
using System.Globalization;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnDatasetValidationEvent
    {
        private readonly ILogger _logger;
        private readonly IDatasetService _datasetService;

        public OnDatasetValidationEvent(
            ILogger logger,
            IDatasetService datasetService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));

            _logger = logger;
            _datasetService = datasetService;
        }

        [FunctionName("on-dataset-validation-event")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.ValidateDataset, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-GB");

            try
            {
                await _datasetService.ValidateDataset(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.ValidateDataset}");
                throw;
            }
        }
    }
}
