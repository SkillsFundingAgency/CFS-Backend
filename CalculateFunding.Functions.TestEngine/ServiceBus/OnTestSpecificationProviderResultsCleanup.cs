using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public class OnTestSpecificationProviderResultsCleanup
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly ITestResultsService _testResultsService;

        public OnTestSpecificationProviderResultsCleanup(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            ITestResultsService testResultsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(testResultsService, nameof(testResultsService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _testResultsService = testResultsService;
        }

        [FunctionName("on-test-specification-provider-results-cleanup")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup,
            ServiceBusConstants.TopicSubscribers.CleanupTestResultsForSpecificationProviders,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _testResultsService.CleanupTestResultsForSpecificationProviders(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup}");
                throw;
            }
        }
    }
}
