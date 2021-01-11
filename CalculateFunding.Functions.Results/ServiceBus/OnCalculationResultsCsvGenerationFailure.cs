using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnCalculationResultsCsvGenerationFailure : Failure
    {
        public const string FunctionName = "on-calculation-results-csv-generation-poisoned";
        private const string QueueName = ServiceBusConstants.QueueNames.CalculationResultsCsvGenerationPoisoned;

        public OnCalculationResultsCsvGenerationFailure(
            ILogger logger,
            IDeadletterService jobHelperService) : base (logger, jobHelperService, QueueName)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                QueueName,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message) => await Process(message);
    }
}