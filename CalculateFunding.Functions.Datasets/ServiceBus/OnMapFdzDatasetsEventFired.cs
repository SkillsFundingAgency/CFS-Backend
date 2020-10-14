using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnMapFdzDatasetsEventFired : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IProcessDatasetService _processDatasetService;

        public const string FunctionName = FunctionConstants.MapFdzDatasets;
        public const string QueueName = ServiceBusConstants.QueueNames.MapFdzDatasets;

        public OnMapFdzDatasetsEventFired(ILogger logger,
            IProcessDatasetService processDatasetService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false)
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(processDatasetService, nameof(processDatasetService));

            _logger = logger;
            _processDatasetService = processDatasetService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _processDatasetService.MapFdzDatasets(message);
                }
                catch (NonRetriableException ex)
                {
                    _logger.Error(ex, $"Job threw non retriable exception: {QueueName}");
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.MapFdzDatasets}");
                    throw;
                }
            },
            message);
        }
    }
}
