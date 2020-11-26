using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Results.UnitTests
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
                .CheckHasNewResultsForSpecificationIdAndTime(
                    Arg.Is("spec-1"),
                    Arg.Any<DateTimeOffset>())
                .Returns(false);

            ILogger logger = CreateLogger();

            IJobManagement jobManagement = CreateJobManagement();

            ResultsService resultsService = CreateResultsService(
                logger,
                specificationsApiClient: specificationsApiClient,
                resultsRepository: calculationResultsRepository,
                jobManagement: jobManagement);

            //Act
            await resultsService.QueueCsvGenerationMessages();

            //Assert
            await
                jobManagement
                    .DidNotReceive()
                    .QueueJob(Arg.Any<JobCreateModel>());

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
                new SpecModel.SpecificationSummary { Id = specificationId }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .CheckHasNewResultsForSpecificationIdAndTime(
                    Arg.Is(specificationId),
                    Arg.Any<DateTimeOffset>())
                .Returns(true);

            ILogger logger = CreateLogger();

            IJobManagement jobManagement = CreateJobManagement();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .DoesBlobExistAsync($"{CalculationResultsReportFilePrefix}-{specificationId}", CalcsResultsContainerName)
                .Returns(true);

            ResultsService resultsService = CreateResultsService(
                logger,
                specificationsApiClient: specificationsApiClient,
                resultsRepository: calculationResultsRepository,
                jobManagement: jobManagement,
                blobClient: blobClient);

            //Act
            await resultsService.QueueCsvGenerationMessages();

            //Assert
            await
                jobManagement
                    .Received(1)
                    .QueueJob(
                    Arg.Is<JobCreateModel>(_ => 
                        _.JobDefinitionId == JobConstants.DefinitionNames.GenerateCalcCsvResultsJob && 
                        _.Properties["specification-id"] == specificationId));

            logger
                .Received()
                .Information($"Found new calculation results for specification id '{specificationId}'");
        }

        [TestMethod]
        public async Task QueueCsvGenerationMessages_GivenSpecificationSummariesFoundAndHasNewResultsForTwoSpecifications_CreatesNewMessage()
        {
            //Arrange

            const string SpecificationOneId = "spec-1";
            const string SpecificationTwoId = "spec-2";

            IEnumerable<SpecModel.SpecificationSummary> specificationSummaries = new[]
            {
                new SpecModel.SpecificationSummary { Id = SpecificationOneId },
                new SpecModel.SpecificationSummary { Id = SpecificationTwoId }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .CheckHasNewResultsForSpecificationIdAndTime(
                    Arg.Is(SpecificationOneId),
                    Arg.Any<DateTimeOffset>())
                .Returns(true);
            calculationResultsRepository
               .CheckHasNewResultsForSpecificationIdAndTime(
                   Arg.Is(SpecificationTwoId),
                   Arg.Any<DateTimeOffset>())
               .Returns(true);

            ILogger logger = CreateLogger();

            IJobManagement jobManagement = CreateJobManagement();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .DoesBlobExistAsync($"{CalculationResultsReportFilePrefix}-{SpecificationOneId}", CalcsResultsContainerName)
                .Returns(true);
            blobClient
                .DoesBlobExistAsync($"{CalculationResultsReportFilePrefix}-{SpecificationTwoId}", CalcsResultsContainerName)
                .Returns(true);

            ResultsService resultsService = CreateResultsService(
                logger,
                specificationsApiClient: specificationsApiClient,
                resultsRepository: calculationResultsRepository,
                jobManagement: jobManagement,
                blobClient: blobClient);

            //Act
            await resultsService.QueueCsvGenerationMessages();

            //Assert
            await
                jobManagement
                    .Received(1)
                    .QueueJob(
                    Arg.Is<JobCreateModel>(_ =>
                        _.JobDefinitionId == JobConstants.DefinitionNames.GenerateCalcCsvResultsJob &&
                        _.Properties["specification-id"] == SpecificationOneId));

            await
                jobManagement
                    .Received(1)
                    .QueueJob(
                    Arg.Is<JobCreateModel>(_ =>
                        _.JobDefinitionId == JobConstants.DefinitionNames.GenerateCalcCsvResultsJob &&
                        _.Properties["specification-id"] == SpecificationTwoId));
            logger
                .Received()
                .Information($"Found new calculation results for specification id '{SpecificationOneId}'");

            logger
               .Received()
               .Information($"Found new calculation results for specification id '{SpecificationTwoId}'");
        }
    }
}
