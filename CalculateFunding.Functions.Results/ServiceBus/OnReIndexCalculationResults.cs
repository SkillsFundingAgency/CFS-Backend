using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnReIndexCalculationResults : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IProviderCalculationResultsReIndexerService _indexerService;
        public const string FunctionName = "on-reindex-calculation-results";

        public OnReIndexCalculationResults(
            ILogger logger,
            IProviderCalculationResultsReIndexerService indexerService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(indexerService, nameof(indexerService));

            _logger = logger;
            _indexerService = indexerService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _indexerService.ReIndexCalculationResults(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.ReIndexCalculationResultsIndex}");
                    throw;
                }
            },
            message);
        }
    }
}
