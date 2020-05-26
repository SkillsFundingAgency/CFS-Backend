using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnCalculationAggregationsJobCompleted : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IJobService _jobService;
        public const string FunctionName = "on-calculation-aggregations-job-completed";

        public OnCalculationAggregationsJobCompleted(
            ILogger logger,
            IJobService jobService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobService, nameof(jobService));

            _logger = logger;
            _jobService = jobService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.JobNotifications,
            ServiceBusConstants.TopicSubscribers.CreateInstructAllocationsJob,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _jobService.CreateInstructAllocationJob(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.JobNotifications}");
                    throw;
                }
            },
            message);
        }
    }
}
