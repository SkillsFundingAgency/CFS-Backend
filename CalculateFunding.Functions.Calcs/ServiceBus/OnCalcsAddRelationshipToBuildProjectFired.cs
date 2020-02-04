using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class CalcsAddRelationshipToBuildProject : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IBuildProjectsService _buildProjectsService;
        public const string FunctionName = "on-calcs-add-data-relationship";

        public CalcsAddRelationshipToBuildProject(
            ILogger logger,
            IBuildProjectsService buildProjectsService,
            IMessengerService messegerService,
            bool isDevelopment = false) : base(logger, messegerService, FunctionName, isDevelopment)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(buildProjectsService, nameof(buildProjectsService));

            _logger = logger;
            _buildProjectsService = buildProjectsService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.UpdateBuildProjectRelationships, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _buildProjectsService.UpdateBuildProjectRelationships(message);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.UpdateBuildProjectRelationships}");
                    throw;
                }
            },
            message);
        }
    }
}
