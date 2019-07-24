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
    public class OnMigrateResultVersionsEvent
    {
        private readonly ILogger _logger;
        private readonly IPublishedResultsService _resultsService;

        public OnMigrateResultVersionsEvent(
            ILogger logger,
            IPublishedResultsService resultsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));

            _logger = logger;
            _resultsService = resultsService;
        }

        [FunctionName("on-migrate-result-versions")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.MigrateResultVersions, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                await _resultsService.MigrateVersionNumbers(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.MigrateResultVersions}");
                throw;
            }
        }
    }
}
