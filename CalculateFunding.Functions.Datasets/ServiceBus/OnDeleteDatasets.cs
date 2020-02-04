using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnDeleteDatasets : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IDatasetService _datasetService;
        public const string FunctionName = "on-delete-datasets";

        public OnDeleteDatasets(
            ILogger logger,
            IDatasetService datasetService,
            IMessengerService messegerService,
            bool isDevelopment = false) : base(logger, messegerService, FunctionName, isDevelopment)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));

            _logger = logger;
            _datasetService = datasetService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeleteDatasets,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
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
            },
            message);
        }
    }
}
