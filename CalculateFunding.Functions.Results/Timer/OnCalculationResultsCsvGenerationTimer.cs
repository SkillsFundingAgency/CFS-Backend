using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Results.Timer
{
    public class OnCalculationResultsCsvGenerationTimer
    {
        private const string Hourly = "0 0 * * * *";

        private readonly ILogger _logger;
        private readonly IResultsService _resultsService;
        private readonly IConfigurationRefresher _configurationRefresher;

        public OnCalculationResultsCsvGenerationTimer(
            ILogger logger,
            IResultsService resultsService,
            IConfigurationRefresherProvider refresherProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));
            Guard.ArgumentNotNull(refresherProvider, nameof(refresherProvider));

            _logger = logger;
            _resultsService = resultsService;

            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName("on-calculation-results-csv-generation-timer")]
        public async Task Run([TimerTrigger(Hourly)]TimerInfo myTimer)
        {
            try
            {
                await _configurationRefresher.TryRefreshAsync();

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
