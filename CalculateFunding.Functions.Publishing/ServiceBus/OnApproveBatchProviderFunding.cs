using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnApproveBatchProviderFunding : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IApproveService _approveService;
        public const string FunctionName = FunctionConstants.PublishingApproveBatchProviderFunding;
        public const string QueueName = ServiceBusConstants.QueueNames.PublishingApproveBatchProviderFunding;

        public OnApproveBatchProviderFunding(
            ILogger logger,
            IApproveService approveService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(approveService, nameof(approveService));

            _logger = logger;
            _approveService = approveService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _approveService.ApproveBatchResults(message);
                }
                catch (NonRetriableException ex)
                {
                    _logger.Error(ex, $"Job threw non retriable exception: {QueueName}");
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from topic: {QueueName}");
                    throw;
                }
            },
            message);
        }
    }
}
