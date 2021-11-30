using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task CreateAdditionalCalculation_GivenValidationFails_ReturnsBadRequest()
        {
            //Arrange
            string correlationId = "any-id";

            CalculationCreateModel model = new CalculationCreateModel();
            Reference author = new Reference();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "oh no an error!!!")
                });

            IValidator<CalculationCreateModel> validator = CreateCalculationCreateModelValidator(validationResult);

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(SpecificationId))
                .Returns(new ApiResponse<SpecificationSummary>(
                    HttpStatusCode.OK,
                    new SpecificationSummary { Id = SpecificationId }
                ));

            CalculationService calculationService = CreateCalculationService(calculationCreateModelValidator: validator, specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await calculationService.CreateAdditionalCalculation(SpecificationId, model, author, correlationId);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task CreateAdditionalCalculation_GivenSavingDraftCalcFails_ReturnsInternalServerErrorResult()
        {
            //Arrange
            string correlationId = "any-id";

            CalculationCreateModel model = CreateCalculationCreateModel();

            Reference author = CreateAuthor();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.BadRequest);

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(SpecificationId))
                .Returns(new ApiResponse<SpecificationSummary>(
                    HttpStatusCode.OK,
                    new SpecificationSummary { Id = SpecificationId }
                ));

            ILogger logger = CreateLogger();

            CalculationService calculationService = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, specificationsApiClient: specificationsApiClient);

            string errorMessage = $"There was problem creating a new calculation with name {CalculationName} in Cosmos Db with status code 400";

            //Act
            IActionResult result = await calculationService.CreateAdditionalCalculation(SpecificationId, model, author, correlationId);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(errorMessage));
        }

        [DataTestMethod]
        [DataRow(true, true)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(false, false)]
        public async Task CreateAdditionalCalculation_GivenCalcSaves_ReturnsOKObjectResult(bool skipQueueCodeContextCacheUpdate, bool skipCalcRun)
        {
            //Arrange
            string cacheKey = $"{CacheKeys.CalculationsMetadataForSpecification}{SpecificationId}";

            CalculationCreateModel model = CreateCalculationCreateModel();

            Reference author = CreateAuthor();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(SpecificationId))
                .Returns(new ApiResponse<SpecificationSummary>(
                    HttpStatusCode.OK,
                    new SpecificationSummary {
                        Id = SpecificationId,
                        FundingPeriod = new FundingPeriod { Id = FundingPeriodId}
                    }
                ));

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            ICodeContextCache codeContextCache = Substitute.For<ICodeContextCache>();

            IResultsApiClient resultsApiClient = CreateResultsApiClient();

            CalculationService calculationService = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                calculationVersionRepository: versionRepository,
                searchRepository: searchRepository,
                jobManagement: jobManagement,
                logger: logger,
                cacheProvider: cacheProvider,
                specificationsApiClient: specificationsApiClient,
                codeContextCache: codeContextCache,
                resultsApiClient: resultsApiClient);

            IEnumerable<CalculationIndex> indexedCalculations = null;

            await
              searchRepository
                  .Index(Arg.Do<IEnumerable<CalculationIndex>>(m =>
                  indexedCalculations = m
                  ));

            CalculationVersion savedCalculationVersion = null;

            await
               versionRepository
                   .SaveVersion(Arg.Do<CalculationVersion>(m => savedCalculationVersion = m));

            //Act
            IActionResult result = await calculationService.CreateAdditionalCalculation(
                SpecificationId, 
                model, 
                author, 
                CorrelationId,
                skipCalcRun: skipCalcRun,
                skipQueueCodeContextCacheUpdate: skipQueueCodeContextCacheUpdate);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>();

            Calculation calculation = (result as OkObjectResult).Value as Calculation;

            if (!skipCalcRun)
            {
                await
                   jobManagement
                       .Received(1)
                       .QueueJob(Arg.Is<JobCreateModel>(
                           m =>
                               m.InvokerUserDisplayName == Username &&
                               m.InvokerUserId == UserId &&
                               m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                               m.Properties["specification-id"] == SpecificationId
                           ));
            
                logger
                   .Received(1)
                   .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: 'job-id-1'"));
            }

            await
                versionRepository
                    .Received(1)
                    .SaveVersion(Arg.Is<CalculationVersion>(m =>
                        m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                        m.Author.Id == UserId &&
                        m.Author.Name == Username &&
                        m.Date.Date == DateTimeOffset.Now.Date &&
                        m.Version == 1 &&
                        m.SourceCode == model.SourceCode &&
                        m.Description == model.Description &&
                        m.ValueType == model.ValueType &&
                        m.CalculationType == CalculationType.Additional &&
                        m.WasTemplateCalculation == false &&
                        m.Namespace == CalculationNamespace.Additional &&
                        m.Name == model.Name &&
                        m.SourceCodeName == new VisualBasicTypeIdentifierGenerator().GenerateIdentifier(model.Name, true) &&
                        m.DataType == CalculationDataType.Decimal
                    ));


            await searchRepository
                .Received(1)
                .Index(Arg.Any<IEnumerable<CalculationIndex>>());

            indexedCalculations
                .Should()
                .BeEquivalentTo(new List<CalculationIndex>()
                {
                    new CalculationIndex()
                    {
                        CalculationType = "Additional",
                        Description = "test description",
                        FundingStreamId ="fs-1",
                        FundingStreamName = model.FundingStreamName,
                        Id = model.Id,
                        Name = model.Name,
                        Namespace = "Additional",
                        SpecificationId = "spec-id-1",
                        SpecificationName = "spec-id-1_specificationName",
                        Status = "Draft",
                        ValueType = "Currency",
                        WasTemplateCalculation = false,
                        LastUpdatedDate = savedCalculationVersion.Date,
                    }
                });

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<List<CalculationMetadata>>(Arg.Is(cacheKey));

            if (!skipQueueCodeContextCacheUpdate)
            {
                await codeContextCache
                    .Received(1)
                    .QueueCodeContextCacheUpdate(SpecificationId);
            }
        }

        [TestMethod]
        public async Task CreateAdditionalCalculation_GivenCreateJobReturnsNull_ReturnsInternalServerError()
        {
            //Arrange
            CalculationCreateModel model = CreateCalculationCreateModel();

            Reference author = CreateAuthor();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .CreateDraftCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns((Job)null);

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(SpecificationId))
                .Returns(new ApiResponse<SpecificationSummary>(
                    HttpStatusCode.OK,
                    new SpecificationSummary { Id = SpecificationId }
                ));

            ILogger logger = CreateLogger();

            CalculationService calculationService = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                calculationVersionRepository: versionRepository,
                searchRepository: searchRepository,
                jobManagement: jobManagement,
                logger: logger,
                specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await calculationService.CreateAdditionalCalculation(SpecificationId, model, author, CorrelationId);

            //Assert
            result
               .Should()
               .BeOfType<InternalServerErrorResult>()
               .Which
               .Value
               .Should()
               .Be($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{SpecificationId}'");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{SpecificationId}'"));
        }
    }
}
