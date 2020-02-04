using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnApproveFunding : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IApproveService _approveService;
        public const string FunctionName = "on-publishing-approve-funding";

        public OnApproveFunding(
            ILogger logger,
            IApproveService approveService,
            IMessengerService messegerService,
            bool isDevelopment = false) : base(logger, messegerService, FunctionName, isDevelopment)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(approveService, nameof(approveService));

            _logger = logger;
            _approveService = approveService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.PublishingApproveFunding,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _approveService.ApproveResults(message);
                }
                catch (NonRetriableException ex)
                {
                    _logger.Error(ex, $"Job threw non retriable exception: {ServiceBusConstants.QueueNames.PublishingApproveFunding}");
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.QueueNames.PublishingApproveFunding}");
                    throw;
                }
            },
            message);
        }
    }
}
