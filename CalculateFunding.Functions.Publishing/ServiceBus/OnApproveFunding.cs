using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnApproveFunding
    {
        private readonly ILogger _logger;
        private readonly IApproveService _approveService;

        public OnApproveFunding(
            ILogger logger,
            ApproveService approveService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(approveService, nameof(approveService));

            _logger = logger;
            _approveService = approveService;
        }

        [FunctionName("on-publishing-approve-funding")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.PublishingApproveFunding,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                await _approveService.ApproveResults(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.QueueNames.PublishingApproveFunding}");
                throw;
            }
        }
    }
}
