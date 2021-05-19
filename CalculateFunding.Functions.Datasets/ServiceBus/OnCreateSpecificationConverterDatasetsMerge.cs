using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Processing.Functions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnCreateSpecificationConverterDatasetsMerge : Retriable
    {
        public const string FunctionName = "on-create-specification-converter-data-merge";
        private const string QueueName = ServiceBusConstants.QueueNames.SpecificationConverterDatasetsMerge;
        
        public OnCreateSpecificationConverterDatasetsMerge(ILogger logger,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            ISpecificationConverterDataMerge specificationConverterDatasetsWizard,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false)
            : base(logger,
                messengerService,
                FunctionName,
                QueueName,
                useAzureStorage,
                userProfileProvider,
                specificationConverterDatasetsWizard,
                refresherProvider)
        {
        }
        
        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                QueueName,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
                IsSessionsEnabled = true)]
            Message message)
        {
            await base.Run(message);
        }
    }
}