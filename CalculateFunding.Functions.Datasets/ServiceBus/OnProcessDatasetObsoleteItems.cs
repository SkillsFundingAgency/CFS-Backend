using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Processing.Functions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnProcessDatasetObsoleteItems : Retriable
    {
        private readonly IDatasetService _datasetService;
        public const string FunctionName = "on-process-dataset-obsolete-items";
        private const string QueueName = ServiceBusConstants.QueueNames.ProcessDatasetObsoleteItems;

        public OnProcessDatasetObsoleteItems(
            ILogger logger,
            IDatasetService datasetService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, datasetService, refresherProvider)
        {
            Guard.ArgumentNotNull(datasetService, nameof(datasetService));

            _datasetService = datasetService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await base.Run(message, async () =>
            {
                await _datasetService.ProcessDatasetObsoleteItems(message);
            });
        }
    }
}
