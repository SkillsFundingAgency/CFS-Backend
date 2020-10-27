using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.DeadletterProcessor;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Providers.ServiceBus
{
    public class OnPopulateScopedProvidersEventTriggerFailure : Failure
    {
        private readonly ILogger _logger;
        private readonly IJobHelperService _jobHelperService;

        public const string FunctionName = FunctionConstants.PopulateScopedProvidersPoisoned;
        public const string QueueName = ServiceBusConstants.QueueNames.PopulateScopedProvidersPoisoned;

        public OnPopulateScopedProvidersEventTriggerFailure(
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
