using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnCreateInstructAllocationLineResultStatusUpdates
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IPublishedResultsService _resultsService;

        public OnCreateInstructAllocationLineResultStatusUpdates(
            ILogger logger,
            IPublishedResultsService resultsService,
            ICorrelationIdProvider correlationIdProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _resultsService = resultsService;
        }

        [FunctionName("on-instruct-allocationline-result-status-updates")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.InstructAllocationLineResultStatusUpdates, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _resultsService.CreateAllocationLineResultStatusUpdateJobs(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.AllocationLineResultStatusUpdates}");
                throw;
            }
        }
    }
}
