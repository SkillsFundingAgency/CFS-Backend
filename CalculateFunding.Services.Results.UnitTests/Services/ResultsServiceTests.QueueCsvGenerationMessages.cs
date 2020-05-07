using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Results.UnitTests.Services
{
    public partial class ResultsServiceTests
    {
        [TestMethod]
        public void QueueCsvGenerationMessages_GivenNoSpecificationSummariesFound_ThrowsRetriableException()
        {
            //Arrange
            string errorMessage = "No specification summaries found to generate calculation results csv.";

            IEnumerable<SpecModel.SpecificationSummary> specificationSummaries = Enumerable.Empty<SpecModel.SpecificationSummary>();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger, specificationsApiClient: specificationsApiClient);

            //Act
            Func<Task> test = async () => await resultsService.QueueCsvGenerationMessages();

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(errorMessage);
        }

        [TestMethod]
        public async Task QueueCsvGenerationMessages_GivenSpecificationSummariesFoundButNoResults_DoesNotCreateNewMessages()
        {
            //Arrange
            IEnumerable<SpecModel.SpecificationSummary> specificationSummaries = new[]
            {
                new SpecModel.SpecificationSummary { Id = "spec-1" }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .CheckHasNewResultsForSpecificationIdAndTimePeriod(
                    Arg.Is("spec-1"),
                    Arg.Any<DateTimeOffset>(),
                    Arg.Any<DateTimeOffset>())
                .Returns(false);

            ILogger logger = CreateLogger();

            IMessengerService messengerService = CreateMessengerService();

            ResultsService resultsService = CreateResultsService(
                logger,
                specificationsApiClient: specificationsApiClient,
                resultsRepository: calculationResultsRepository,
                messengerService: messengerService);

            //Act
            await resultsService.QueueCsvGenerationMessages();

            //Assert
            await
                messengerService
                    .DidNotReceive()
                    .SendToQueue(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<bool>());

            logger
                .DidNotReceive()
                .Information($"Found new calculation results for specification id 'spec-1'");
        }

        [TestMethod]
        public async Task QueueCsvGenerationMessages_GivenSpecificationSummariesFoundAndHasNewResults_CreatesNewMessage()
        {
            //Arrange
            IEnumerable<SpecModel.SpecificationSummary> specificationSummaries = new[]
            {
                new SpecModel.SpecificationSummary { Id = "spec-1" }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .CheckHasNewResultsForSpecificationIdAndTimePeriod(
                    Arg.Is("spec-1"),
                    Arg.Any<DateTimeOffset>(),
                    Arg.Any<DateTimeOffset>())
                .Returns(true);

            ILogger logger = CreateLogger();

            IMessengerService messengerService = CreateMessengerService();

            ResultsService resultsService = CreateResultsService(
                logger,
                specificationsApiClient: specificationsApiClient,
                resultsRepository: calculationResultsRepository,
                messengerService: messengerService);

            //Act
            await resultsService.QueueCsvGenerationMessages();

            //Assert
            await
                messengerService
                    .Received(1)
                    .SendToQueue(
                        Arg.Is(ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration),
                        Arg.Is(string.Empty),
                        Arg.Is<Dictionary<string, string>>(m => m["specification-id"] == "spec-1"), Arg.Is(false));

            logger
                .Received()
                .Information($"Found new calculation results for specification id 'spec-1'");
        }

        [TestMethod]
        public async Task QueueCsvGenerationMessages_GivenSpecificationSummariesFoundAndHasNewResultsForTwoSpecifications_CreatesNewMessage()
        {
            //Arrange
            IEnumerable<SpecModel.SpecificationSummary> specificationSummaries = new[]
            {
                new SpecModel.SpecificationSummary { Id = "spec-1" },
                new SpecModel.SpecificationSummary { Id = "spec-2" }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .CheckHasNewResultsForSpecificationIdAndTimePeriod(
                    Arg.Is("spec-1"),
                    Arg.Any<DateTimeOffset>(),
                    Arg.Any<DateTimeOffset>())
                .Returns(true);
            calculationResultsRepository
               .CheckHasNewResultsForSpecificationIdAndTimePeriod(
                   Arg.Is("spec-2"),
                   Arg.Any<DateTimeOffset>(),
                   Arg.Any<DateTimeOffset>())
               .Returns(true);

            ILogger logger = CreateLogger();

            IMessengerService messengerService = CreateMessengerService();

            ResultsService resultsService = CreateResultsService(
                logger,
                specificationsApiClient: specificationsApiClient,
                resultsRepository: calculationResultsRepository,
                messengerService: messengerService);

            //Act
            await resultsService.QueueCsvGenerationMessages();

            //Assert
            await
                messengerService
                    .Received(1)
                    .SendToQueue(
                        Arg.Is(ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration),
                        Arg.Is(string.Empty),
                        Arg.Is<Dictionary<string, string>>(m => m["specification-id"] == "spec-1"), Arg.Is(false));

            await
               messengerService
                   .Received(1)
                   .SendToQueue(
                       Arg.Is(ServiceBusConstants.QueueNames.CalculationResultsCsvGeneration),
                       Arg.Is(string.Empty),
                       Arg.Is<Dictionary<string, string>>(m => m["specification-id"] == "spec-2"), Arg.Is(false));
            logger
                .Received()
                .Information($"Found new calculation results for specification id 'spec-1'");

            logger
               .Received()
               .Information($"Found new calculation results for specification id 'spec-2'");
        }
    }
}
