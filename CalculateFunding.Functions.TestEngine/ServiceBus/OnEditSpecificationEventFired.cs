using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.TestEngine.ServiceBus
{
    public static class OnEditSpecificationEvent
    {
        [FunctionName("on-edit-specification")]
        public static async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditSpecification,
            ServiceBusConstants.TopicSubscribers.UpdateScenarioResultsForEditSpecification,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            using (var scope = IocConfig.Build().CreateScope())
            {
                var correlationIdProvider = scope.ServiceProvider.GetService<ICorrelationIdProvider>();
                var testResultsService = scope.ServiceProvider.GetService<ITestResultsService>();
                var logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {
                    correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                    await testResultsService.UpdateTestResultsForSpecification(message);
                }
                catch (Exception exception)
                {
                    logger.Error(exception, $"An error occurred getting message from topic: {ServiceBusConstants.TopicNames.EditSpecification}");
                    throw;
                }

            }
        }
    }
}
