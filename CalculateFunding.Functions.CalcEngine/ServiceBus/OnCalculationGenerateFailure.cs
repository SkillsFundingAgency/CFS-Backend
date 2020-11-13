using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.CalcEngine.ServiceBus
{
    public class OnCalculationGenerateFailure : Failure
    {
        private const string QueueName = ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResultsPoisoned;

        public OnCalculationGenerateFailure(
            ILogger logger,
            IDeadletterService jobHelperService) : base (logger, jobHelperService, QueueName)
        {
        }

        /// <summary>
        /// On poisoned message for running calcs
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        [FunctionName("on-calcs-generate-allocations-event-poisoned")]
        public async Task Run([ServiceBusTrigger(QueueName, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message) => await Process(message);
    }
}
