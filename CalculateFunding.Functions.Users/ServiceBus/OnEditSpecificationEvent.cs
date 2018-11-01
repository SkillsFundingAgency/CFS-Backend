using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Users.ServiceBus
{
    public static class OnEditSpecificationEvent
    {
        [FunctionName("users-on-edit-specification")]
        public static async Task Run([ServiceBusTrigger(
            ServiceBusConstants.TopicNames.EditSpecification,
            ServiceBusConstants.TopicSubscribers.UpdateUsersForEditSpecification,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            IConfigurationRoot config = ConfigHelper.AddConfig();

            using (var scope = IocConfig.Build(config).CreateScope())
            {
                IFundingStreamPermissionService service = scope.ServiceProvider.GetService<IFundingStreamPermissionService>();
                Serilog.ILogger logger = scope.ServiceProvider.GetService<Serilog.ILogger>();

                try
                {
                    await service.OnSpecificationUpdate(message);
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
