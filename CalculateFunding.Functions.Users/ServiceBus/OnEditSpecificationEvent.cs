using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Users.ServiceBus
{
    public class OnEditSpecificationEvent : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IFundingStreamPermissionService _fundingStreamPermissionService;
        public const string FunctionName = "users-on-edit-specification";

        public OnEditSpecificationEvent(
            ILogger logger,
            IFundingStreamPermissionService fundingStreamPermissionService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(fundingStreamPermissionService, nameof(fundingStreamPermissionService));

            _logger = logger;
            _fundingStreamPermissionService = fundingStreamPermissionService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditSpecification,
            ServiceBusConstants.TopicSubscribers.UpdateUsersForEditSpecification,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
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
            },
            message);
        }
    }
}
