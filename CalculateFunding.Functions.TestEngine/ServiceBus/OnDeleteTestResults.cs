using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public class OnDeleteTestResults
    {
        private readonly ILogger _logger;
        private readonly ITestResultsService _testResultsService;

        public OnDeleteTestResults(
            ILogger logger,
            ITestResultsService testResultsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(testResultsService, nameof(testResultsService));

            _logger = logger;
            _testResultsService = testResultsService;
        }

        [FunctionName("on-delete-test-results")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeleteTestResults,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                await _testResultsService.DeleteTestResults(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.DeleteTestResults}");
                throw;
            }
        }
    }
}
