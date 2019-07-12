using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Users.ServiceBus
{
    public class OnEditSpecificationEvent
    {
        private readonly ILogger _logger;
        private readonly IFundingStreamPermissionService _fundingStreamPermissionService;

        public OnEditSpecificationEvent(
            ILogger logger,
            IFundingStreamPermissionService fundingStreamPermissionService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(fundingStreamPermissionService, nameof(fundingStreamPermissionService));

            _logger = logger;
            _fundingStreamPermissionService = fundingStreamPermissionService;
        }

        [FunctionName("users-on-edit-specification")]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditSpecification,
            ServiceBusConstants.TopicSubscribers.UpdateUsersForEditSpecification,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                await _fundingStreamPermissionService.OnSpecificationUpdate(message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.EditSpecification}");
                throw;
            }
        }
    }
}
