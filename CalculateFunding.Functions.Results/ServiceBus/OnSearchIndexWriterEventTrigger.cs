using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnSearchIndexWriterEventTrigger : Retriable
    {
        public const string FunctionName = FunctionConstants.SearchIndexWriter;
        public const string QueueName = ServiceBusConstants.QueueNames.SearchIndexWriter;

        public OnSearchIndexWriterEventTrigger(ILogger logger,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            ISearchIndexWriterService searchIndexWriterService,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false)
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, searchIndexWriterService, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await base.Run(message);
        }
    }
}
