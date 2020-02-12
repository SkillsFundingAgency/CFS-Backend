using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public class OnTestExecution : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly ITestEngineService _testEngineService;
        public const string FunctionName = "on-test-execution-event";

        public OnTestExecution(
            ILogger logger,
            ITestEngineService testEngineService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(testEngineService, nameof(testEngineService));

            _logger = logger;
            _testEngineService = testEngineService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.TestEngineExecuteTests, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _testEngineService.RunTests(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred processing message on queue: '{ServiceBusConstants.QueueNames.TestEngineExecuteTests}'");
                    throw;
                }
            },
            message);
        }
    }
}
