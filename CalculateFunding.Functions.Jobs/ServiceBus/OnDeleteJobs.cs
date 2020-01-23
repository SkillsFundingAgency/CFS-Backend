using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Jobs.ServiceBus
{
    public class OnDeleteJobs
    {
        private readonly ILogger _logger;
        private readonly IJobManagementService _jobManagementService;

        public OnDeleteJobs(
            ILogger logger,
            IJobManagementService jobManagementService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobManagementService, nameof(jobManagementService));

            _logger = logger;
            _jobManagementService = jobManagementService;
        }

        [FunctionName("on-delete-jobs")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeleteJobs,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                await _jobManagementService.DeleteJobs(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.DeleteJobs}");
                throw;
            }
        }
    }
}
