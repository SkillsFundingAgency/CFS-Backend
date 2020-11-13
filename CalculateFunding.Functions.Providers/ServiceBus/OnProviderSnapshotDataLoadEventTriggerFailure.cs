using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Providers.ServiceBus
{
    public class OnProviderSnapshotDataLoadEventTriggerFailure : Failure
    {
        public const string FunctionName = FunctionConstants.ProviderSnapshotDataLoadPoisoned;
        public const string QueueName = ServiceBusConstants.QueueNames.ProviderSnapshotDataLoadPoisoned;

        public OnProviderSnapshotDataLoadEventTriggerFailure(
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
