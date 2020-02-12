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
    public class OnCalculationResultsCsvGeneration : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IProviderResultsCsvGeneratorService _csvGeneratorService;
        public const string FunctionName = "on-calculation-results-csv-generation";

        public OnCalculationResultsCsvGeneration(
            ILogger logger,
            IProviderResultsCsvGeneratorService csvGeneratorService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(csvGeneratorService, nameof(csvGeneratorService));

            _logger = logger;
            _csvGeneratorService = csvGeneratorService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] 
            Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _csvGeneratorService.Run(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration}");
                
                    throw;
                }
            },
            message);
        }
    }
}

