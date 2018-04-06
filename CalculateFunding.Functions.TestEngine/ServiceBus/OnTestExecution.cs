using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public static class OnTestExecution
    {
        [FunctionName("on-test-execution-event")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.TestEngineExecuteTests, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var testEngineService = scope.ServiceProvider.GetService<ITestEngineService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {

                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await testEngineService.RunTests(message);

                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.TestEngineExecuteTests}");
                    throw;
                }

            }
        }
    }
}
