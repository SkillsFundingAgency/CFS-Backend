using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnMapFdzDatasetsEventFiredFailure : Failure
    {
        public const string FunctionName = FunctionConstants.MapFdzDatasetsPoisoned;
        public const string QueueName = ServiceBusConstants.QueueNames.MapFdzDatasetsPoisoned;

        public OnMapFdzDatasetsEventFiredFailure(
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
