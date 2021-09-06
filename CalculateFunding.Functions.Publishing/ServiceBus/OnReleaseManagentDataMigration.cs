using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnReleaseManagementDataMigration : Retriable
    {
        private const string FunctionName = FunctionConstants.ReleaseManagementDataMigration;
        private const string QueueName = ServiceBusConstants.QueueNames.ReleaseManagementDataMigration;

        public OnReleaseManagementDataMigration(
            ILogger logger,
            IPublishingV3ToSqlMigrator publishingV3ToSqlMigrator,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, publishingV3ToSqlMigrator, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.ReleaseManagementDataMigration,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
                IsSessionsEnabled = true)] 
            Message message)
        {
            await base.Run(message);
        }
    }
}