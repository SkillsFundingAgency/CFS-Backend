using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnDeletePublishedProviders
    {
        private const string PublishingDeletePublishedProviders = ServiceBusConstants.QueueNames.DeletePublishedProviders;
        
        private readonly ILogger _logger;
        private readonly IDeletePublishedProvidersService _deletePublishedProvidersService;

        public OnDeletePublishedProviders(
            ILogger logger,
            IDeletePublishedProvidersService deletePublishedProvidersService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(deletePublishedProvidersService, nameof(deletePublishedProvidersService));

            _logger = logger;
            _deletePublishedProvidersService = deletePublishedProvidersService;
        }

        [FunctionName("on-publishing-delete-published-providers")]
        public async Task Run([ServiceBusTrigger(
            PublishingDeletePublishedProviders,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] 
            Message message)
        {
            try
            {
                await _deletePublishedProvidersService.DeletePublishedProvidersJob(message);
            }
            catch (NonRetriableException ex)
            {
                _logger.Error(ex, $"Job threw non retriable exception: {PublishingDeletePublishedProviders}");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from topic: {PublishingDeletePublishedProviders}");
                
                throw;
            }
        }
    }
}
