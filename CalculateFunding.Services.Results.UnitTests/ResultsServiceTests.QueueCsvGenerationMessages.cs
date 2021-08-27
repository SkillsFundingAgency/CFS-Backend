using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;
using CalcModels = CalculateFunding.Common.ApiClient.Calcs.Models;

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
        public async Task QueueCsvGenerationMessages_GivenMissingTemplateMappings_LogsError()
        {
            //Arrange
            string fundingStreamId = new RandomString();
            string specificationId = new RandomString();

            IEnumerable<SpecModel.SpecificationSummary> specificationSummaries = new[]
            {
                new SpecModel.SpecificationSummary {
                    Id = specificationId,
                    FundingStreams = new[] { NewReference(_ => _.WithId(fundingStreamId)) }
                }
            };

            string errorMessage = $"Specification: {specificationId} has missing calculations in template mapping";

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

            ICalculationsApiClient calculationsApiClient = CreateCalculationsApiClient();
            calculationsApiClient
                .GetTemplateMapping(specificationId, fundingStreamId)
                .Returns(new ApiResponse<CalcModels.TemplateMapping>(HttpStatusCode.OK,
                    new CalcModels.TemplateMapping {
                        FundingStreamId = fundingStreamId,
                        SpecificationId = specificationId,
                        TemplateMappingItems = new[] { 
                            new CalcModels.TemplateMappingItem()
                        }
                    }));

            ILogger logger = CreateLogger();

            ResultsService resultsService = CreateResultsService(logger, specificationsApiClient: specificationsApiClient, calculationsApiClient: calculationsApiClient);

            //Act
            await resultsService.QueueCsvGenerationMessages();

            //Assert
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
                new SpecModel.SpecificationSummary { 
                    Id = "spec-1",
                    FundingStreams = new[] { NewReference() }
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .ProviderHasResultsBySpecificationId(
                    Arg.Is("spec-1"))
                .Returns(false);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .DoesBlobExistAsync("funding-lines-spec-1-Released.csv", CsvReportsContainerName)
                .Returns(false);

            ILogger logger = CreateLogger();

            IJobManagement jobManagement = CreateJobManagement();

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
                new SpecModel.SpecificationSummary { 
                    Id = SpecificationId ,
                    FundingStreams = new[] { NewReference() }
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaries()
                .Returns(new ApiResponse<IEnumerable<SpecModel.SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

            ICalculationResultsRepository calculationResultsRepository = CreateResultsRepository();
            calculationResultsRepository
                .CheckHasNewResultsForSpecificationIdAndTime(
                    Arg.Is(SpecificationId),
                    Arg.Any<DateTimeOffset>())
                .Returns(true);

            ILogger logger = CreateLogger();

            IJobManagement jobManagement = CreateJobManagement();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .DoesBlobExistAsync($"calculation-results-{SpecificationId}.csv", CsvReportsContainerName)
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
                        _.Properties["specification-id"] == SpecificationId));

            logger
                .Received()
                .Information($"Found new calculation results for specification id '{SpecificationId}'");
        }

        [TestMethod]
        public async Task QueueCsvGenerationMessages_GivenSpecificationSummariesFoundAndHasNewResultsForTwoSpecifications_CreatesNewMessage()
        {
            //Arrange

            const string SpecificationOneId = "spec-1";
            const string SpecificationTwoId = "spec-2";

            IEnumerable<SpecModel.SpecificationSummary> specificationSummaries = new[]
            {
                new SpecModel.SpecificationSummary { 
                    Id = SpecificationOneId,
                    FundingStreams = new[] { NewReference() }
                },
                new SpecModel.SpecificationSummary { 
                    Id = SpecificationTwoId ,
                    FundingStreams = new[] { NewReference() }
                }
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
                .DoesBlobExistAsync($"calculation-results-{SpecificationOneId}.csv", CsvReportsContainerName)
                .Returns(true);
            blobClient
                .DoesBlobExistAsync($"calculation-results-{SpecificationTwoId}.csv", CsvReportsContainerName)
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
