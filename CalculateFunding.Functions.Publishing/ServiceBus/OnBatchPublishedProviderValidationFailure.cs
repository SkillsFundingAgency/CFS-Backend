using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnBatchPublishedProviderValidationFailure : Failure
    {
        private const string FunctionName = FunctionConstants.BatchPublishedProviderValidationPoisoned;
        private const string QueueName = ServiceBusConstants.QueueNames.PublishingBatchPublishedProviderValidationPoisoned;

        public OnBatchPublishedProviderValidationFailure(
            ILogger logger,
            IDeadletterService jobHelperService) : base(logger, jobHelperService, QueueName)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message) => await Process(message);
    }
}
