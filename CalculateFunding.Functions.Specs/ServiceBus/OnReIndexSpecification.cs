using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
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
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider, specificationIndexing)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationIndexing, nameof(specificationIndexing));

            _logger = logger;
            _specificationIndexing = specificationIndexing;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(ReIndexSingleSpecification, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await base.Run(message);
        }
    }
}
