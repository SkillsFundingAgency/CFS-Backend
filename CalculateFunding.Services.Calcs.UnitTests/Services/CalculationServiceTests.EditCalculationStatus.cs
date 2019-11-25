using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task EditCalculationStatus_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.UpdateCalculationStatus(null, null);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation Id was provided to EditCalculationStatus"));
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenNullEditModeldWasProvided_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.UpdateCalculationStatus(CalculationId, null);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null status model provided");

            logger
                .Received(1)
                .Error(Arg.Is("A null status model was provided"));
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenCalculationWasNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            ILogger logger = CreateLogger();

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();
            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: CalculationsRepository);

            //Act
            IActionResult result = await service.UpdateCalculationStatus(CalculationId, CalculationEditStatusModel);

            //Arrange
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Calculation not found");

            logger
                .Received(1)
                .Warning(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }


        [TestMethod]
        public async Task EditCalculationStatus_GivenCurrentCalculationWasNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            ILogger logger = CreateLogger();

            Calculation calculation = new Calculation();

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();
            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: CalculationsRepository);

            //Act
            IActionResult result = await service.UpdateCalculationStatus(CalculationId, CalculationEditStatusModel);

            //Arrange
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"A current calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenStatusHasntChanges_DoesNotUpdateReturnsOkResult()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            ILogger logger = CreateLogger();

            Calculation calculation = CreateCalculation();
            calculation.Current.PublishStatus = PublishStatus.Approved;

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();
            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: CalculationsRepository);

            //Act
            IActionResult result = await service.UpdateCalculationStatus(CalculationId, CalculationEditStatusModel);

            //Arrange
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(calculation.Current);

            await
                CalculationsRepository
                .DidNotReceive()
                .UpdateCalculation(Arg.Any<Calculation>());
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenNewStatusButNoSpecSummaryFound_ReturnsPreConditionFailed()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            ILogger logger = CreateLogger();

            Calculation calculation = CreateCalculation();

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();

            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, null));

            CalculationService service = CreateCalculationService(
                logger: logger, calculationsRepository: CalculationsRepository, specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await service.UpdateCalculationStatus(CalculationId, CalculationEditStatusModel);

            //Arrange
            result
                .Should()
                .BeAssignableTo<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("Specification not found");
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenNewStatusButUpdatingDbReturnsBadRequest_ReturnsStatusCode400()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            ILogger logger = CreateLogger();

            Calculation calculation = CreateCalculation();

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();

            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.BadRequest);

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));


            CalculationService service = CreateCalculationService(
                logger: logger, calculationsRepository: CalculationsRepository, specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await service.UpdateCalculationStatus(CalculationId, CalculationEditStatusModel);

            //Arrange
            result
                .Should()
                .BeAssignableTo<StatusCodeResult>()
                .Which
                .StatusCode
                .Should()
                .Be(400);
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenNewStatus_UpdatesSearchReturnsOK()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Approved
            };

            ILogger logger = CreateLogger();

            Calculation calculation = CreateCalculation();

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Approved;

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();

            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Name = "spec name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            BuildProject buildProject = new BuildProject();

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Models.Calcs.Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            CalculationService service = CreateCalculationService(
                logger: logger, calculationsRepository: CalculationsRepository, searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient, calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService, buildProjectsService: buildProjectsService);

            //Act
            IActionResult result = await service.UpdateCalculationStatus(CalculationId, CalculationEditStatusModel);

            //Arrange
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<PublishStatusResultModel>()
                .Which
                .PublishStatus
                .Should()
                .Be(PublishStatus.Approved);

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Approved);
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenCalculationIsApprovedButNewStatusIsDraft_UpdatesSearchReturnsOK()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Draft
            };

            ILogger logger = CreateLogger();

            Calculation calculation = CreateCalculation();
            calculation.Current.PublishStatus = PublishStatus.Approved;

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();

            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);


            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary();

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            CalculationService service = CreateCalculationService(
                logger: logger, calculationsRepository: CalculationsRepository, searchRepository: searchRepository, specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await service.UpdateCalculationStatus(CalculationId, CalculationEditStatusModel);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Publish status can't be changed to Draft from Updated or Approved");

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Approved);

            await
                searchRepository
                .Received(0)
                .Index(Arg.Any<IEnumerable<CalculationIndex>>());
        }

        [TestMethod]
        public async Task EditCalculationStatus_GivenNewStatusOfUpdated_UpdatesSearchReturnsOK()
        {
            //Arrange
            EditStatusModel CalculationEditStatusModel = new EditStatusModel
            {
                PublishStatus = PublishStatus.Updated
            };

            ILogger logger = CreateLogger();

            Calculation calculation = CreateCalculation();
            calculation.Current.PublishStatus = PublishStatus.Approved;

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            ICalculationsRepository CalculationsRepository = CreateCalculationsRepository();

            CalculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);


            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Name = "spec name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            BuildProject buildProject = new BuildProject();

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Models.Calcs.Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            CalculationService service = CreateCalculationService(
                logger: logger, calculationsRepository: CalculationsRepository, searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient, calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService, buildProjectsService: buildProjectsService);

            //Act
            IActionResult result = await service.UpdateCalculationStatus(CalculationId, CalculationEditStatusModel);

            //Arrange
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<PublishStatusResultModel>()
                .Which
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);
        }
    }
}
