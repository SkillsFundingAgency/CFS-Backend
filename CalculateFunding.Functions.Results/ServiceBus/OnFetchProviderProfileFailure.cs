using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnFetchProviderProfileFailure
    {
        private readonly ILogger _logger;
        private readonly IJobHelperService _jobHelperService;

        public OnFetchProviderProfileFailure(
            ILogger logger,
            IJobHelperService jobHelperService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobHelperService, nameof(jobHelperService));

            _logger = logger;
            _jobHelperService = jobHelperService;
        }

        [FunctionName("on-fetch-provider-profile-poisoned")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.FetchProviderProfilePoisoned, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]Message message)
        {
            try
            {
                await _jobHelperService.ProcessDeadLetteredMessage(message);

                _logger.Information("Proccessed fetch provider profile dead lettered message complete");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.FetchProviderProfilePoisoned}");
                throw;
            }
        }
    }
}
