using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Datasets.ServiceBus
{
    public class OnDatasetEventFailure
    {
        private readonly ILogger _logger;
        private readonly IJobHelperService _jobHelperService;

        public OnDatasetEventFailure(
            ILogger logger,
            IJobHelperService jobHelperService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobHelperService, nameof(jobHelperService));

            _logger = logger;
            _jobHelperService = jobHelperService;
        }

        [FunctionName("on-dataset-event-poisoned")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.ProcessDatasetPoisoned, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                await _jobHelperService.ProcessDeadLetteredMessage(message);

                _logger.Information("Starting to process dead letter message for dataset event");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.ProcessDatasetPoisoned}");
                throw;
            }
        }
    }
}
