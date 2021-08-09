using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnProcessDatasetObsoleteItemsFailure : Failure
    {
        public const string FunctionName = "on-process-dataset-obsolete-items-poisoned";
        private const string QueueName = ServiceBusConstants.QueueNames.ProcessDatasetObsoleteItemsPoisoned;

        public OnProcessDatasetObsoleteItemsFailure(
            ILogger logger,
            IDeadletterService jobHelperService,
            IConfigurationRefresherProvider refresherProvider) : base(logger, jobHelperService, QueueName, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(QueueName, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message) => await Process(message);
    }
}
