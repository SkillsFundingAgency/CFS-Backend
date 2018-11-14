using CalculateFunding.Services.Core.Constants;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.CalcEngine.ServiceBus
{
    public static class OnCalculationGenerateFailure
    {
        /// <summary>
        /// On poisoned message for running calcs
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        [FunctionName("OnCalculationGenerateFailure")]
        public static void Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResultsPoisoned, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]Message message, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {message}");

            // Send JobLog to Jobs service detailing failed calculations
        }
    }
}
