using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public class OnDeleteTestResults : Retriable
    {
        private readonly ITestResultsService _testResultsService;
        private const string FunctionName = "on-delete-test-results";
        private const string QueueName = ServiceBusConstants.QueueNames.DeleteTestResults;

        public OnDeleteTestResults(
            ILogger logger,
            ITestResultsService testResultsService,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, QueueName, useAzureStorage, userProfileProvider, testResultsService)
        {
            Guard.ArgumentNotNull(testResultsService, nameof(testResultsService));

            _testResultsService = testResultsService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.DeleteTestResults,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await base.Run(message,
                async() =>
                {
                    await _testResultsService.DeleteTestResults(message);
                });
        }
    }
}
