using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnUpdateCodeContextCacheFailure : Failure
    {
        private const string FunctionName = "on-update-code-context-cache-poisoned";
        private const string QueueName = ServiceBusConstants.QueueNames.UpdateCodeContextCachePoisoned;

        public OnUpdateCodeContextCacheFailure(
            ILogger logger,
            IDeadletterService jobHelperService) : base (logger, jobHelperService, QueueName)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.UpdateCodeContextCachePoisoned,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message) => await Process(message);
    }
}