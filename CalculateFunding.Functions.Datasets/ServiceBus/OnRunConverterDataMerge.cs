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
    public class OnRunConverterDataMerge : Retriable
    {
        public const string FunctionName = "on-run-converter-data-merge";
        private const string DatasetsConverterDatasetMerge = ServiceBusConstants.QueueNames.RunConverterDatasetMerge;

        public OnRunConverterDataMerge(
            ILogger logger,
            IConverterDataMergeService converterDataMergeService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false)
            : base(logger,
                messengerService,
                FunctionName,
                DatasetsConverterDatasetMerge,
                useAzureStorage,
                userProfileProvider,
                converterDataMergeService,
                refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                DatasetsConverterDatasetMerge,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
                IsSessionsEnabled = true)]
            Message message)
        {
            await base.Run(message);
        }
    }
}