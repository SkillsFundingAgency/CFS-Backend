using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.CalcEngine.ServiceBus
{
    public class OnCalculationGenerateFailure
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IJobHelperService _jobHelperService;

        public OnCalculationGenerateFailure(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            IJobHelperService jobHelperService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(jobHelperService, nameof(jobHelperService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _jobHelperService = jobHelperService;
        }

        /// <summary>
        /// On poisoned message for running calcs
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        [FunctionName("on-calcs-generate-allocations-event-poisoned")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResultsPoisoned, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]Message message)
        {
            _logger.Information("Scope created, starting to process dead letter message for generating allocations");

            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _jobHelperService.ProcessDeadLetteredMessage(message);

                _logger.Information("Proccessed generate allocations dead lettered message complete");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred processing message on queue: {ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResultsPoisoned}");
                throw;
            }

        }
    }
}
