﻿using System;
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
    public class OnDeleteTestResults : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly ITestResultsService _testResultsService;
        public const string FunctionName = "on-delete-test-results";

        public OnDeleteTestResults(
            ILogger logger,
            ITestResultsService testResultsService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(testResultsService, nameof(testResultsService));

            _logger = logger;
            _testResultsService = testResultsService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeleteTestResults,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
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
            },
            message);
        }
    }
}