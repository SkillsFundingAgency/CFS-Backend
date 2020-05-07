using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Calcs.ServiceBus
{
    public class OnCalcsInstructAllocationResults : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IBuildProjectsService _buildProjectsService;
        public const string FunctionName = "on-calcs-instruct-allocations";

        public OnCalcsInstructAllocationResults(
            ILogger logger,
            IBuildProjectsService buildProjectsService,
            IMessengerService messegerService,
            bool useAzureStorage = false) : base(logger, messegerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(buildProjectsService, nameof(buildProjectsService));

            _logger = logger;
            _buildProjectsService = buildProjectsService;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
            ServiceBusConstants.QueueNames.CalculationJobInitialiser,
            Connection = ServiceBusConstants.ConnectionStringConfigurationKey)] Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _buildProjectsService.UpdateAllocations(message);
                }
                catch (NonRetriableException ex)
                {
                    _logger.Error(ex, $"An error occurred processing message in queue, non retry: {ServiceBusConstants.QueueNames.CalculationJobInitialiser}");
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"An error occurred processing message in queue: {ServiceBusConstants.QueueNames.CalculationJobInitialiser}");
                    throw;
                }
            },
            message);
        }
    }

}
