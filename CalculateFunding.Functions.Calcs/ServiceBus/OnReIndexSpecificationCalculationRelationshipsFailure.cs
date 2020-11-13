using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnReIndexSpecificationCalculationRelationshipsFailure : Failure
    {
        public const string FunctionName = "on-reindex-specification-calculation-relationships-poisoned";
        private const string QueueName = ServiceBusConstants.QueueNames.ReIndexSpecificationCalculationRelationshipsPoisoned;

        public OnReIndexSpecificationCalculationRelationshipsFailure(
            ILogger logger,
            IDeadletterService jobHelperService) : base (logger, jobHelperService, QueueName)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.ReIndexSpecificationCalculationRelationshipsPoisoned,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message) => await Process(message);
    }
}