using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.DeadletterProcessor;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnApproveAllCalculationsFailure : Failure
    {
        private const string FunctionName = "on-approve-all-calculations-poisoned";
        private const string QueueName = ServiceBusConstants.QueueNames.ApproveAllCalculationsPoisoned;

        public OnApproveAllCalculationsFailure(
            ILogger logger,
            IJobHelperService jobHelperService) : base(logger, jobHelperService, QueueName)
        {
        }

        [FunctionName(FunctionName)]
        public async Task Run(
            [ServiceBusTrigger(QueueName, 
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] 
            Message message) => await Process(message);
    }
}
