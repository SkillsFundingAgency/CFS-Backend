using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnPopulateCalculationResultsQADatabaseFailure : Failure
    {
        private const string FunctionName = FunctionConstants.PopulateCalculationResultsQaDatabasePoisoned;
        private const string QueueName = ServiceBusConstants.QueueNames.PopulateCalculationResultsQADatabasePoisoned;

        public OnPopulateCalculationResultsQADatabaseFailure(
            ILogger logger,
            IDeadletterService jobHelperService,
            IConfigurationRefresherProvider refresherProvider) : base(logger, jobHelperService, QueueName, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                QueueName,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message) => await Process(message);
    }
}
