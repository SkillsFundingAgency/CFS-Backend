using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnGeneratePublishedProviderEstateCsv : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IPublishedProviderEstateCsvGenerator _csvGenerator;

        private const string FunctionName = "on-publishing-generate-published-provider-estate-csv";

        public OnGeneratePublishedProviderEstateCsv(
            ILogger logger,
            IPublishedProviderEstateCsvGenerator csvGenerator,
            IMessengerService messengerService,
            IUserProfileProvider userProfileProvider, bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage, userProfileProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(csvGenerator, nameof(csvGenerator));

            _logger = logger;
            _csvGenerator = csvGenerator;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.GeneratePublishedProviderEstateCsv,
                Connection = ServiceBusConstants.ConnectionStringConfigurationKey)]
                Message message)
        {
            await Run(async () =>
            {
                try
                {
                    await _csvGenerator.Run(message);
                }
                catch (NonRetriableException e)
                {
                    _logger.Error(e, $"Unable to complete job {FunctionName}");
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Encountered error completing job {FunctionName}");

                    throw;
                }
            },
                message);
        }
    }
}
