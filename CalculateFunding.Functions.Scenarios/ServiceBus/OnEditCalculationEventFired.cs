using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Scenarios.ServiceBus
{
    public class OnEditCalculationEvent : SmokeTest
    {
        public const string FunctionName = "on-edit-calculation-for-scenarios";
        private readonly IConfigurationRefresher _configurationRefresher;

        public OnEditCalculationEvent(
            ILogger logger,
            IScenariosService scenariosService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider, scenariosService)
        {
            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditCalculation,
            ServiceBusConstants.TopicSubscribers.UpdateScenariosForEditCalculation,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await _configurationRefresher.TryRefreshAsync();

            await base.Run(message);
        }
    }
}
