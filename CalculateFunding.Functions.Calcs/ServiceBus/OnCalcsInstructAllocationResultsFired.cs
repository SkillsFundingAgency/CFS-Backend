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
    public class OnCalcsInstructAllocationResults
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IBuildProjectsService _buildProjectsService;

        public OnCalcsInstructAllocationResults(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            IBuildProjectsService buildProjectsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(buildProjectsService, nameof(buildProjectsService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _buildProjectsService = buildProjectsService;
        }

        [FunctionName("on-calcs-instruct-allocations")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.CalculationJobInitialiser, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _buildProjectsService.UpdateAllocations(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.CalculationJobInitialiser}");
                throw;
            }
        }
    }

}
