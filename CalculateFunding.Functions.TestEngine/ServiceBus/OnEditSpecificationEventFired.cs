using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public class OnEditSpecificationEvent : SmokeTest
    {
        private readonly ITestResultsService _testResultsService;
        private readonly IConfigurationRefresher _configurationRefresher;

        public const string FunctionName = "on-edit-specification";

        public OnEditSpecificationEvent(
            ILogger logger,
            ITestResultsService testResultsService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider, testResultsService)
        {
            Guard.ArgumentNotNull(testResultsService, nameof(testResultsService));
            Guard.ArgumentNotNull(refresherProvider, nameof(refresherProvider));

            _testResultsService = testResultsService;
            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditSpecification,
            ServiceBusConstants.TopicSubscribers.UpdateScenarioResultsForEditSpecification,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await _configurationRefresher.TryRefreshAsync();

            await base.Run(message);
        }
    }
}
