using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnApproveAllProviderFundingFailure : Failure
    {
        public const string FunctionName = FunctionConstants.PublishingApproveAllProviderFundingPoisoned;
        public const string QueueName = ServiceBusConstants.QueueNames.PublishingApproveAllProviderFundingPoisoned;

        public OnApproveAllProviderFundingFailure(
            ILogger logger,
            IDeadletterService jobHelperService) : base(logger, jobHelperService, QueueName)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            QueueName,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message) => await Process(message);
    }
}
