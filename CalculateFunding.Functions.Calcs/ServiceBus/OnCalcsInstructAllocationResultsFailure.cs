﻿using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnCalcsInstructAllocationResultsFailure : Failure
    {
        private const string FunctionName = "on-calcs-instruct-allocations-poisoned";
        private const string QueueName = ServiceBusConstants.QueueNames.CalculationJobInitialiserPoisoned;

        public OnCalcsInstructAllocationResultsFailure(
            ILogger logger,
            IDeadletterService jobHelperService,
            IConfigurationRefresherProvider refresherProvider) : base (logger, jobHelperService, QueueName, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CalculationJobInitialiserPoisoned, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message) => await Process(message);
    }
}
