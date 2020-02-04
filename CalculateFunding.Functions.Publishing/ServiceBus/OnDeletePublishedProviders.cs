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
    public class OnDeletePublishedProviders : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IDeletePublishedProvidersService _deletePublishedProvidersService;
        public const string FunctionName = "on-publishing-delete-published-providers";

        public OnDeletePublishedProviders(
            ILogger logger,
            IDeletePublishedProvidersService deletePublishedProvidersService,
            IMessengerService messegerService,
            bool isDevelopment = false) : base(logger, messegerService, FunctionName, isDevelopment)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(deletePublishedProvidersService, nameof(deletePublishedProvidersService));

            _logger = logger;
            _deletePublishedProvidersService = deletePublishedProvidersService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeletePublishedProviders,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] 
            Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _deletePublishedProvidersService.DeletePublishedProvidersJob(message);
                }
                catch (NonRetriableException ex)
                {
                    _logger.Error(ex, $"Job threw non retriable exception: {ServiceBusConstants.QueueNames.DeletePublishedProviders}");
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.QueueNames.DeletePublishedProviders}");
                
                    throw;
                }
            },
            message);
        }
    }
}
