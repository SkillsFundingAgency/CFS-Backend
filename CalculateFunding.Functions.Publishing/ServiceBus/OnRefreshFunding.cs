using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnRefreshFunding
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IRefreshService _refreshService;

        public OnRefreshFunding(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            IRefreshService refreshService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(refreshService, nameof(refreshService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _refreshService = refreshService;
        }

        [FunctionName("on-publishing-refresh-funding")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.PublishingRefreshFunding,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());

            try
            {
                await _refreshService.RefreshResults(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.QueueNames.PublishingRefreshFunding}");
                throw;
            }
        }
    }
}
