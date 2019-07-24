﻿using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.CalcEngine.ServiceBus
{
    public class OnCalcsGenerateAllocationResults
    {
        private readonly ILogger _logger;
        private readonly ICalculationEngineService _calculationEngineService;

        public OnCalcsGenerateAllocationResults(
            ILogger logger,
            ICalculationEngineService calculationEngineService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calculationEngineService, nameof(calculationEngineService));

            _logger = logger;
            _calculationEngineService = calculationEngineService;
        }

        [FunctionName("on-calcs-generate-allocations-event")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            _logger.Information("Scope created, starting to generate allocations");

            try
            {
                await _calculationEngineService.GenerateAllocations(message);

                _logger.Information("Generate allocations complete");
            }
            catch (NonRetriableException nrEx)
            {
                _logger.Error(nrEx, $"An error occurred processing message on queue: {ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults}");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred processing message on queue: {ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults}");
                throw;
            }
        }
    }
}
