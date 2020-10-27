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
    public class OnUpdateCodeContextCacheFailure : Failure
    {
        private readonly IJobHelperService _jobHelperService;
        private const string FunctionName = "on-update-code-context-cache-poisoned";
        private const string QueueName = ServiceBusConstants.QueueNames.UpdateCodeContextCachePoisoned;

        public OnUpdateCodeContextCacheFailure(
            ILogger logger,
            IJobHelperService jobHelperService) : base (logger, jobHelperService, QueueName)
        {
            Guard.ArgumentNotNull(jobHelperService, nameof(jobHelperService));

            _jobHelperService = jobHelperService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.UpdateCodeContextCachePoisoned,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message) => await Process(message);
    }
}