using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnCalculationAggregationsJobCompleted
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IJobService _jobService;

        public OnCalculationAggregationsJobCompleted(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            IJobService jobService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(jobService, nameof(jobService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _jobService = jobService;
        }

        [FunctionName("on-calculation-aggregations-job-completed")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.JobNotifications,
            ServiceBusConstants.TopicSubscribers.CreateInstructAllocationsJob,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _jobService.CreateInstructAllocationJob(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.JobNotifications}");
                throw;
            }
        }
    }
}
