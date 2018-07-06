using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public static class OnTestExecution
    {
        [FunctionName("on-test-execution-event")]
        public static async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.TestEngineExecuteTests, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            using (IServiceScope scope = IocConfig.Build(message).CreateScope())
            {
                ICorrelationIdProvider correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                ITestEngineService testEngineService = scope.ServiceProvider.GetService<ITestEngineService>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await testEngineService.RunTests(message);

                }
                catch (Exception exception)
                {
                    ILogger logger = scope.ServiceProvider.GetService<ILogger>();
                    logger.Error(exception, $"An error occurred processing message on queue: '{ServiceBusConstants.QueueNames.TestEngineExecuteTests}'");
                    throw;
                }

            }
        }
    }
}
