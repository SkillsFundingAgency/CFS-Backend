using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.DeadletterProcessor;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnApplyTemplateCalculationsFailure : Failure
    {
        private const string FunctionName = "on-apply-template-calculations-poisoned";
        private const string QueueName = ServiceBusConstants.QueueNames.ApplyTemplateCalculationsPoisoned;

        public OnApplyTemplateCalculationsFailure(
            ILogger logger,
            IJobHelperService jobHelperService) : base (logger, jobHelperService, QueueName)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(QueueName, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message) => await Process(message);
    }
}
