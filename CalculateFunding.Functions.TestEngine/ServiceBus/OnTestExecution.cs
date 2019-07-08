using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public class OnTestExecution
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly ITestEngineService _testEngineService;

        public OnTestExecution(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            ITestEngineService testEngineService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(testEngineService, nameof(testEngineService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _testEngineService = testEngineService;
        }

        [FunctionName("on-test-execution-event")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.TestEngineExecuteTests, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _testEngineService.RunTests(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred processing message on queue: '{ServiceBusConstants.QueueNames.TestEngineExecuteTests}'");
                throw;
            }
        }
    }
}
