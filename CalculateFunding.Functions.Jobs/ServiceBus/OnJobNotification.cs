using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Jobs.ServiceBus
{
    public class OnJobNotification : Retriable
    {
        public const string FunctionName = "on-job-notification";

        public OnJobNotification(
            ILogger logger,
            IJobManagementService jobManagementService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false)
            : base(logger, messengerService, FunctionName, $"{ServiceBusConstants.TopicNames.JobNotifications}/{ServiceBusConstants.TopicSubscribers.UpdateJobsOnCompletion}", useAzureStorage, userProfileProvider, jobManagementService)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.JobNotifications,
            ServiceBusConstants.TopicSubscribers.UpdateJobsOnCompletion,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await base.Run(message);
        }
    }
}
