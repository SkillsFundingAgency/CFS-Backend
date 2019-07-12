using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Jobs.ServiceBus
{
    public class OnJobNotification
    {
        private readonly ILogger _logger;
        private readonly IJobManagementService _jobManagementService;
        private readonly ICorrelationIdProvider _correlationIdProvider;

        public OnJobNotification(
            ILogger logger,
            IJobManagementService jobManagementService,
            ICorrelationIdProvider correlationIdProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobManagementService, nameof(jobManagementService));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));

            _logger = logger;
            _jobManagementService = jobManagementService;
            _correlationIdProvider = correlationIdProvider;
        }

        [FunctionName("on-job-notification")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.JobNotifications,
            ServiceBusConstants.TopicSubscribers.UpdateJobsOnCompletion,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _jobManagementService.ProcessJobNotification(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.JobNotifications}");
                throw;
            }
            
        }
    }
}
