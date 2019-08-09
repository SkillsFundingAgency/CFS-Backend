using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnCalculationResultsCsvGeneration
    {
        private readonly ILogger _logger;
        private readonly IResultsService _resultsService;

        public OnCalculationResultsCsvGeneration(
            ILogger logger,
            IResultsService resultsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));

            _logger = logger;
            _resultsService = resultsService;
        }

        [FunctionName("on-calculation-results-csv-generation")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                await _resultsService.GenerateCalculationResultsCsv(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration}");
                throw;
            }
        }
    }
}

