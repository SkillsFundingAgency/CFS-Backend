using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.DeadletterProcessor;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnApproveBatchProviderFundingFailure : Failure
    {
        private const string FunctionName = FunctionConstants.PublishingApproveBatchProviderFundingPoisoned;
        private const string QueueName = ServiceBusConstants.QueueNames.PublishingApproveBatchProviderFundingPoisoned;

        public OnApproveBatchProviderFundingFailure(
            ILogger logger,
            IJobHelperService jobHelperService) : base(logger, jobHelperService, QueueName)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message) => await Process(message);
    }
}
