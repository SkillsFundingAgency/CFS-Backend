using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Serilog;

namespace CalculateFunding.Functions.Publishing.ServiceBus
{
    public class OnGeneratePublishedFundingCsv : SmokeTest
    {
        private readonly ILogger _logger;
        private readonly IFundingLineCsvGenerator _csvGenerator;

        private const string FunctionName = "on-publishing-generate-published-funding-csv";

        public OnGeneratePublishedFundingCsv(
            ILogger logger,
            IFundingLineCsvGenerator csvGenerator,
            IMessengerService messengerService,
            bool useAzureStorage = false) 
            : base(logger, messengerService, FunctionName, useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(csvGenerator, nameof(csvGenerator));

            _logger = logger;
            _csvGenerator = csvGenerator;
        }

        [FunctionName(FunctionName)]
        public async Task Run([ServiceBusTrigger(
                ServiceBusConstants.QueueNames.GeneratePublishedFundingCsv,
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