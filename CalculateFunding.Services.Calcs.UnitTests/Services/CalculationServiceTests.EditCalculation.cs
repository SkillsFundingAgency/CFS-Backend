using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task EditCalculation_GivenValidationFails_ReturnsBadRequest()
        {
            //Arrange
            string correlationId = "any-id";

            CalculationEditModel model = new CalculationEditModel();
            Reference author = new Reference();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "oh no an error!!!")
                });

            IValidator<CalculationEditModel> validator = CreateCalculationEditModelValidator(validationResult);

            CalculationService calculationService = CreateCalculationService(calculationEditModelValidator: validator);

            //Act
            IActionResult result = await calculationService.EditCalculation(SpecificationId, CalculationId, model, author, correlationId);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationExistsWithNoHistory_CreatesNewVersion()
        {
            //Arrange
            Calculation calculation = CreateCalculation();

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = new Reference();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            BuildProject buildProject = new BuildProject();

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobsApiClient: jobsApiClient);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
        }

        [TestMethod]
        public async Task EditCalculation_GivenModelButCalculationDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            ILogger logger = CreateLogger();

            Reference author = new Reference();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationExistsWithButBuildProjectDoesNotExist_CreatesNewBuildProject()
        {
            // Arrange
            string cacheKey = $"{CacheKeys.CalculationsMetadataForSpecification}{SpecificationId}";

            string buildProjectId = Guid.NewGuid().ToString();

            Calculation calculation = CreateCalculation();

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = new Reference();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            BuildProject buildProject = new BuildProject();

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            ICacheProvider cacheProvider = CreateCacheProvider();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobsApiClient: jobsApiClient,
                buildProjectsService: buildProjectsService,
                cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));

            await
              sourceCodeService
                  .Received(1)
                  .SaveAssembly(Arg.Any<BuildProject>());

            await
                sourceCodeService
                    .Received(1)
                    .SaveSourceFiles(Arg.Is<IEnumerable<SourceFile>>(m => m.Count() == 1), Arg.Is(calculation.SpecificationId), Arg.Is(SourceCodeType.Release));

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<List<CalculationMetadata>>(Arg.Is(cacheKey));
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationsCreatedUpdatesBuildProject()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject();

            Calculation calculation = CreateCalculation();

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = new Reference();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobsApiClient: jobsApiClient);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));

            await
               sourceCodeService
                   .Received(1)
                   .SaveAssembly(Arg.Any<BuildProject>());

            await
                sourceCodeService
                    .Received(1)
                    .SaveSourceFiles(Arg.Is<IEnumerable<SourceFile>>(m => m.Count() == 1), Arg.Is(calculation.SpecificationId), Arg.Is(SourceCodeType.Release));
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationSpecificationDescriptionSetWithSingleCalculation()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject();

            string specificationId = "789";

            List<Calculation> calcCalculations = new List<Calculation>();

            Calculation calculation = CreateCalculation();
            calculation.SpecificationId = specificationId;

            calcCalculations.Add(calculation);

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = new Reference();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .GetCalculationsBySpecificationId(specificationId)
                .Returns(calcCalculations.AsEnumerable());

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = Substitute.For<ISourceFileGeneratorProvider>();
            sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic)
                .Returns(sourceFileGenerator);

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobsApiClient: jobsApiClient);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await calculationsRepository
                .Received(1)
                .UpdateCalculation(Arg.Is<Calculation>(c => c.Current.Description == calculation.Current.Description));

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));

            await
               sourceCodeService
                   .Received(1)
                   .SaveAssembly(Arg.Any<BuildProject>());

            await
                sourceCodeService
                    .Received(1)
                    .SaveSourceFiles(Arg.Is<IEnumerable<SourceFile>>(m => m.Count() == 1), Arg.Is(specificationId), Arg.Is(SourceCodeType.Release));
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationExistsWithBuildIdButNoCalculations_EnsuresCalculationSpecificationDescriptionSetWithMultipleCalculations()
        {
            //Arrange
            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = new Reference();

            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject();

            string specificationId = "789";

            List<Calculation> calcCalculations = new List<Calculation>();

            Calculation calculation = CreateCalculation();
            calculation.SpecificationId = specificationId;


            calcCalculations.Add(calculation);

            Calculation calculation2 = CreateCalculation();
            calculation2.Id = "12555";
            calculation2.SpecificationId = specificationId;

            calcCalculations.Add(calculation2);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationsBySpecificationId(specificationId)
                .Returns(calcCalculations.AsEnumerable());

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            ISourceFileGenerator sourceFileGenerator = Substitute.For<ISourceFileGenerator>();

            ISourceFileGeneratorProvider sourceFileGeneratorProvider = Substitute.For<ISourceFileGeneratorProvider>();
            sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic)
                .Returns(sourceFileGenerator);

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobsApiClient: jobsApiClient);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await calculationsRepository
                .Received(1)
                .UpdateCalculation(Arg.Is<Calculation>(c => c.Current.Description == calculation.Current.Description));

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationExistsWithBuildIdButCalculationCouldNotBeFound_AddsCalculationUpdatesBuildProject()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject();

            Calculation calculation = CreateCalculation();

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = new Reference();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobsApiClient: jobsApiClient);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationExistsWithBuildIdButButNotInSearch_CreatesSearchDocument()
        {
            //Arrange
            Calculation calculation = CreateCalculation();

            string buildProjectId = Guid.NewGuid().ToString();
            string specificationId = calculation.SpecificationId;

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
                SpecificationId = specificationId
            };

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = new Reference();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            List<SourceFile> sourceFiles = new List<SourceFile>
            {
                new SourceFile { FileName = "test.vb", SourceCode = "any content"}
            };

            Build build = new Build
            {
                Success = true,
                SourceFiles = sourceFiles,
                Assembly = new byte[100]
            };

            buildProject.Build = build;

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns((CalculationIndex)null);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsService: buildProjectsService,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository,
               calculationVersionRepository: versionRepository,
               sourceCodeService: sourceCodeService,
               jobsApiClient: jobsApiClient);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Any<IList<CalculationIndex>>());

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));

            await
                sourceCodeService
                    .Received(1)
                    .SaveAssembly(Arg.Is(buildProject));
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationIsCurrentlyPublished_SetsPublishStateToUpdated()
        {
            //Arrange
            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = new Reference();

            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.Current.PublishStatus = PublishStatus.Approved;

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(calculation.SpecificationId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobsApiClient: jobsApiClient);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));

            await
                sourceCodeService
                    .Received(1)
                    .SaveAssembly(Arg.Is(buildProject));
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationIsCurrentlyUpdated_SetsPublishStateToUpdated()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.SpecificationId = specificationId;

            calculation.Current.PublishStatus = PublishStatus.Updated;

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = new Reference();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobsApiClient: jobsApiClient);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(calculationVersion));

            await
                sourceCodeService
                    .Received(1)
                    .SaveAssembly(Arg.Is(buildProject));
        }

        [TestMethod]
        public async Task EditCalculation_SetsPublishStateToUpdatedAddNewJob()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
                SpecificationId = specificationId
            };

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = CreateAuthor();

            Calculation calculation = CreateCalculation();
            calculation.SpecificationId = specificationId;

            calculation.Current.PublishStatus = PublishStatus.Updated;

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsService: buildProjectsService,
               searchRepository: searchRepository,
               specificationRepository: specificationRepository,
               calculationVersionRepository: versionRepository,
               jobsApiClient: jobsApiClient,
               sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            await
                jobsApiClient
                    .Received(1)
                    .CreateJob(Arg.Is<JobCreateModel>(
                        m =>
                            m.InvokerUserDisplayName == Username &&
                            m.InvokerUserId == UserId &&
                            m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                            m.Properties["specification-id"] == specificationId &&
                            m.Trigger.EntityId == CalculationId &&
                            m.Trigger.EntityType == nameof(Calculation) &&
                            m.Trigger.Message == $"Saving calculation: '{CalculationId}' for specification: '{calculation.SpecificationId}'"
                        ));

            logger
               .Received(1)
               .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: 'job-id-1'"));

            await
                sourceCodeService
                    .Received(1)
                    .SaveAssembly(Arg.Is(buildProject));
        }

        [TestMethod]
        public async Task EditCalculation_GivenCreatingJobReturnsNull_LogsErrorReturnsInternalServerError()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = CreateAuthor();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
                SpecificationId = specificationId
            };

            Calculation calculation = CreateCalculation();
            calculation.SpecificationId = specificationId;

            calculation.Current.PublishStatus = PublishStatus.Updated;

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns((Job)null);

            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                jobsApiClient: jobsApiClient,
                sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'");

            await
                jobsApiClient
                    .Received(1)
                    .CreateJob(Arg.Is<JobCreateModel>(
                        m =>
                            m.InvokerUserDisplayName == Username &&
                            m.InvokerUserId == UserId &&
                            m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                            m.Properties["specification-id"] == specificationId &&
                            m.Trigger.EntityId == CalculationId &&
                            m.Trigger.EntityType == nameof(Calculation) &&
                            m.Trigger.Message == $"Saving calculation: '{CalculationId}' for specification: '{calculation.SpecificationId}'"
                        ));

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'"));
        }

        [TestMethod]
        public void EditCalculation_GivenCalculationUpdateFails_ThenExceptionIsThrown()
        {
            //Arrange
            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = CreateAuthor();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.SpecificationId = specificationId;

            calculation.Current.PublishStatus = PublishStatus.Updated;

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.InternalServerError);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                 buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository);

            //Act
            Func<Task<IActionResult>> resultFunc = async () => await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            resultFunc
                .Should().Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be("Update calculation returned status code 'InternalServerError' instead of OK");
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalcsContainCalculationAggregates_AddsNewJobToAggregateCalculations()
        {
            //Arrange
            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation
                {
                    Current = new CalculationVersion
                    {
                        SourceCode = "return Sum(Calc1)"
                    }
                }
            };

            string buildProjectId = Guid.NewGuid().ToString();

            string specificationId = Guid.NewGuid().ToString();

            CalculationEditModel calculationEditModel = CreateCalculationEditModel();

            Reference author = CreateAuthor();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
                SpecificationId = specificationId
            };

            Calculation calculation = CreateCalculation();
            calculation.SpecificationId = specificationId;

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            calculationsRepository
                .UpdateCalculation(Arg.Any<Calculation>())
                .Returns(HttpStatusCode.OK);

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            IBuildProjectsService buildProjectsService = CreateBuildProjectsService();
            buildProjectsService
                .GetBuildProjectForSpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            CalculationIndex calcIndex = new CalculationIndex();

            ISearchRepository<CalculationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .SearchById(Arg.Is(CalculationId))
                .Returns(calcIndex);

            Models.Specs.SpecificationSummary specificationSummary = new Models.Specs.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(specificationSummary);

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            Build build = new Build
            {
                SourceFiles = new List<SourceFile> { new SourceFile() }
            };

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                 buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationRepository: specificationRepository,
                calculationVersionRepository: versionRepository,
                jobsApiClient: jobsApiClient,
                sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            await
                jobsApiClient
                    .Received(1)
                    .CreateJob(Arg.Is<JobCreateModel>(
                        m =>
                            m.InvokerUserDisplayName == Username &&
                            m.InvokerUserId == UserId &&
                            m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob &&
                            m.Properties["specification-id"] == specificationId &&
                            m.Trigger.EntityId == CalculationId &&
                            m.Trigger.EntityType == nameof(Calculation) &&
                            m.Trigger.Message == $"Saving calculation: '{CalculationId}' for specification: '{calculation.SpecificationId}'"
                        ));

            logger
               .Received(1)
               .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: 'job-id-1'"));

            await
                sourceCodeService
                    .Received(1)
                    .SaveAssembly(Arg.Is(buildProject));

            await
                sourceCodeService
                    .Received(1)
                    .SaveSourceFiles(Arg.Is<IEnumerable<SourceFile>>(m => m.Count() == 1), Arg.Is(specificationId), Arg.Is(SourceCodeType.Release));
        }
    }
}
