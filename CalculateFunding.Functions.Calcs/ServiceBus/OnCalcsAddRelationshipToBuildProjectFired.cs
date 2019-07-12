using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class CalcsAddRelationshipToBuildProject
    {
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IBuildProjectsService _buildProjectsService;

        public CalcsAddRelationshipToBuildProject(
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider,
            IBuildProjectsService buildProjectsService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(correlationIdProvider, nameof(correlationIdProvider));
            Guard.ArgumentNotNull(buildProjectsService, nameof(buildProjectsService));

            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
            _buildProjectsService = buildProjectsService;
        }

        [FunctionName("on-calcs-add-data-relationship")]
        public async Task Run([ServiceBusTrigger(ServiceBusConstants.QueueNames.UpdateBuildProjectRelationships, Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            try
            {
                _correlationIdProvider.SetCorrelationId(message.GetCorrelationId());
                await _buildProjectsService.UpdateBuildProjectRelationships(message);

            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"An error occurred getting message from queue: {ServiceBusConstants.QueueNames.UpdateBuildProjectRelationships}");
                throw;
            }
            
        }
    }
}
