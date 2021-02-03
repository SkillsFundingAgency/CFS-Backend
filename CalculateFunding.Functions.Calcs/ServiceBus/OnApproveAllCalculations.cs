using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnApproveAllCalculations : Retriable
    {
        public const string FunctionName = "on-approve-all-calculations";
        public const string QueueName = ServiceBusConstants.QueueNames.ApproveAllCalculations;

        public OnApproveAllCalculations(
            ILogger logger,
            IApproveAllCalculationsService approveAllCalculationsService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false)
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, approveAllCalculationsService, refresherProvider)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey, IsSessionsEnabled = true)] Message message)
        {
            await base.Run(message);
        }
    }
}
