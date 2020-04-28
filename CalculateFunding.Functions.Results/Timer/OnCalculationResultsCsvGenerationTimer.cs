using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Results.Timer
{
    public class OnCalculationResultsCsvGenerationTimer
    {
        private const string Hourly = "0 0 * * * *";

        private readonly ILogger _logger;
        private readonly IResultsService _resultsService;

        public OnCalculationResultsCsvGenerationTimer(
            ILogger logger,
            IResultsService resultsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));

            _logger = logger;
            _resultsService = resultsService;
        }

        [FunctionName("on-calculation-results-csv-generation-timer")]
        public async Task Run([TimerTrigger(Hourly)]TimerInfo myTimer)
        {
            try
            {
                await _resultsService.QueueCsvGenerationMessages();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "An error occurred getting message from timer job: on-calculation-results-csv-generation-timer");
                throw;
            }
        }
    }
}
