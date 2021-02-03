using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnDeleteCalculations : Retriable
    {
        private readonly ICalculationService _calculationService;
        private const string FunctionName = "on-delete-calculations";
        private const string QueueName = ServiceBusConstants.QueueNames.DeleteCalculations;

        public OnDeleteCalculations(
            ILogger logger,
            ICalculationService calculationService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider,
            IConfigurationRefresherProvider refresherProvider,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, calculationService, refresherProvider)
        {
            Guard.ArgumentNotNull(calculationService, nameof(calculationService));

            _calculationService = calculationService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeleteCalculations,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await base.Run(message, async() =>
            {
                await _calculationService.DeleteCalculations(message);
            });
        }
    }
}
