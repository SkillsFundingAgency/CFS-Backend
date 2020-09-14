using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.DeadletterProcessor;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnUpdateCodeContextCacheFailure
    {
        private readonly ILogger _logger;
        private readonly IJobHelperService _jobHelperService;
        public const string FunctionName = "on-update-code-context-cache-poisoned";

        public OnUpdateCodeContextCacheFailure(
            ILogger logger,
            IJobHelperService jobHelperService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobHelperService, nameof(jobHelperService));

            _logger = logger;
            _jobHelperService = jobHelperService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.UpdateCodeContextCachePoisoned,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
            Message message)
        {
            _logger.Information("Starting to process dead letter message for update code context cache.");

            try
            {
                await _jobHelperService.ProcessDeadLetteredMessage(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.UpdateCodeContextCachePoisoned}");

                throw;
            }
        }
    }
}