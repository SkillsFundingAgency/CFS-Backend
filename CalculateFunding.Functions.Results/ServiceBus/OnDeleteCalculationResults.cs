using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Results.ServiceBus
{
    public class OnDeleteCalculationResults : Retriable
    {
        private readonly IResultsService _resultsService;
        private const string FunctionName = "on-delete-calculation-results";
        private const string QueueName = ServiceBusConstants.QueueNames.DeleteCalculationResults;

        public OnDeleteCalculationResults(
            ILogger logger,
            IResultsService resultsService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false)
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, resultsService)
        {
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));

            _resultsService = resultsService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await base.Run(message, async () =>
            {
                await _resultsService.DeleteCalculationResults(message);
            });
        }
    }
}
