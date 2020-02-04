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
    public class OnDatasetEvent : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IProcessDatasetService _processDatasetService;
        public const string FunctionName = "on-dataset-event";

        public OnDatasetEvent(
            ILogger logger,
            IProcessDatasetService processDatasetService,
            IMessengerService messegerService,
            bool isDevelopment = false) : base(logger, messegerService, FunctionName, isDevelopment)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(processDatasetService, nameof(processDatasetService));

            _logger = logger;
            _processDatasetService = processDatasetService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.ProcessDataset,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _processDatasetService.ProcessDataset(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.ProcessDataset}");
                    throw;
                }
            },
            message);
        }
    }
}
