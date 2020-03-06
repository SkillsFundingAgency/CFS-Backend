﻿using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnDeleteCalculationResults : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly ICalculationService _calculationService;
        public const string FunctionName = "on-delete-calculation-results";

        public OnDeleteCalculationResults(
            ILogger logger,
            ICalculationService calculationService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationService, nameof(calculationService));

            _logger = logger;
            _calculationService = calculationService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeleteCalculationResults,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _calculationService.DeleteCalculationResults(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.DeleteCalculationResults}");
                    throw;
                }
            },
            message);
        }
    }
}