using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Specs.ServiceBus
{
    public class OnReIndexSpecification : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly ISpecificationIndexingService _specificationIndexing;
        
        public const string FunctionName = "on-reindex-specification";
        private const string ReIndexSingleSpecification = ServiceBusConstants.QueueNames.ReIndexSingleSpecification;

        public OnReIndexSpecification(
            ILogger logger,
            ISpecificationIndexingService specificationIndexing,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationIndexing, nameof(specificationIndexing));

            _logger = logger;
            _specificationIndexing = specificationIndexing;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(ReIndexSingleSpecification, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _specificationIndexing.Run(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ReIndexSingleSpecification}");
                    throw;
                }
            },
            message);
        }
    }
}
