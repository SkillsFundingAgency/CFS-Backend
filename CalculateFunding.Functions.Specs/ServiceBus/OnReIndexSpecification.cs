using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Specs.ServiceBus
{
    public class OnReIndexSpecification : SmokeTest
    {
        private readonly IConfigurationRefresher _configurationRefresher;

        public const string FunctionName = "on-reindex-specification";
        private const string ReIndexSingleSpecification = ServiceBusConstants.QueueNames.ReIndexSingleSpecification;

        public OnReIndexSpecification(
            ILogger logger,
            ISpecificationIndexingService specificationIndexing,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider, specificationIndexing)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationIndexing, nameof(specificationIndexing));
            Guard.ArgumentNotNull(refresherProvider, nameof(refresherProvider));

            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(ReIndexSingleSpecification, 
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey,
            IsSessionsEnabled = true)] Message message)
        {
            await _configurationRefresher.TryRefreshAsync();

            await base.Run(message);
        }
    }
}
