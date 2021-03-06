﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingPeriod = new Reference
                {
                    Id = new RandomString()
                },
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            IResultsApiClient resultsApiClient = CreateResultsApiClient();

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement,
                buildProjectsService: buildProjectsService,
                cacheProvider: cacheProvider,
                policiesApiClient: policiesApiClient,
                resultsApiClient: resultsApiClient);

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

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

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                specificationsApiClient: specificationsApiClient,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

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

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                specificationsApiClient: specificationsApiClient,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
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

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsService: buildProjectsService,
               searchRepository: searchRepository,
               specificationsApiClient: specificationsApiClient,
               calculationVersionRepository: versionRepository,
               sourceCodeService: sourceCodeService,
               jobManagement: jobManagement);

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
               buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
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
               specificationsApiClient: specificationsApiClient,
               calculationVersionRepository: versionRepository,
               jobManagement: jobManagement,
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
                jobManagement
                    .Received(1)
                    .QueueJob(Arg.Is<JobCreateModel>(
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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
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
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                jobManagement: jobManagement,
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
                jobManagement
                    .Received(1)
                    .QueueJob(Arg.Is<JobCreateModel>(
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
        public async Task EditCalculation_GivenCalculationUpdateFails_ThenExceptionIsThrown()
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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                 buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient);

            //Act           
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
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
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                jobManagement: jobManagement,
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
                jobManagement
                    .Received(1)
                    .QueueJob(Arg.Is<JobCreateModel>(
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

        [TestMethod]
        public async Task EditCalculation_GivenCalcsContainCalculationAggregatesButSkipInstruct_NewJobToAggregateCalculationsNotAdded()
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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            IVersionRepository<CalculationVersion> versionRepository = CreateCalculationVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<CalculationVersion>(), Arg.Any<CalculationVersion>())
                .Returns(calculationVersion);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
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
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository,
                jobManagement: jobManagement,
                sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId, skipInstruct: true);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            calculation
                .Current
                .PublishStatus
                .Should()
                .Be(PublishStatus.Updated);

            jobManagement
                    .DidNotReceive();

        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationUpdateFailsWithNoSpecificationSummary_ThenExceptionIsThrown()
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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, null));

            CalculationService service = CreateCalculationService(
                logger: logger,
                calculationsRepository: calculationsRepository,
                 buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient);

            //Act          

            IActionResult result = await service.EditCalculation(SpecificationId, CalculationId, calculationEditModel, author, CorrelationId);

            //Assert
            result
                .Should().BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"No specification with id {calculation.SpecificationId}. Unable to get Specification Summary for calculation");
        }

        [TestMethod]
        public async Task EditCalculation_GivenCalculationHasExistingEnumValuesAndUserIsUpdatingSourceCodeForCalc_ThenExistingEnumValuesAreKept()
        {
            //Arrange
            CalculationEditModel calculationEditModel = new CalculationEditModel
            {
                Name = CalculationName,
                ValueType = CalculationValueType.String,
                SourceCode = DefaultSourceCode,
                Description = Description,
                DataType = CalculationDataType.Enum,
                AllowedEnumTypeValues = null, // Assumed client will not pass these as they can't override them
                CalculationId = CalculationId,
                SpecificationId = SpecificationId,
            };

            Reference author = new Reference();

            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.Current.CalculationType = CalculationType.Template;
            calculation.Current.DataType = CalculationDataType.Enum;
            calculation.Current.ValueType = CalculationValueType.String;
            calculation.Current.AllowedEnumTypeValues = new[] { "One", "Two", "Three" };
            calculation.Current.Namespace = CalculationNamespace.Template;


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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            Mock<IVersionRepository<CalculationVersion>> versionRepository = new Mock<IVersionRepository<CalculationVersion>>();

            CalculationVersion createdCalculationVersion = null;

            versionRepository
                .Setup(m => m.CreateVersion(It.IsAny<CalculationVersion>(), It.IsAny<CalculationVersion>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Callback((CalculationVersion newVersion, CalculationVersion currentVersion, string partitionKey, bool incrementFromCurrentVersion) =>
                {
                    createdCalculationVersion = newVersion;
                })
                .ReturnsAsync(calculationVersion);


            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository.Object,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

            //Act
            IActionResult result = await service.EditCalculation(
                SpecificationId,
                CalculationId,
                calculationEditModel,
                author,
                CorrelationId,
                setAdditional: false,
               calculationEditMode: CalculationEditMode.User);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            createdCalculationVersion
                .AllowedEnumTypeValues
                .Should()
                .BeEquivalentTo(new string[] { "One", "Two", "Three" });

            createdCalculationVersion
                .DataType
                .Should()
                .Be(CalculationDataType.Enum);

            createdCalculationVersion
                .Should()
                .BeEquivalentTo(new CalculationVersion()
                {
                    AllowedEnumTypeValues = new string[] { "One", "Two", "Three" },
                    Author = author,
                    CalculationId = CalculationId,
                    CalculationType = CalculationType.Template,
                    Comment = null,
                    DataType = CalculationDataType.Enum,
                    Description = null, // Value not stored in cosmos
                    Name = "Test Calc Name",
                    Namespace = CalculationNamespace.Template,
                    PublishStatus = PublishStatus.Updated,
                    SourceCode = DefaultSourceCode,
                    SourceCodeName = "TestCalcName",
                    ValueType = CalculationValueType.String,
                    Version = 1,
                    WasTemplateCalculation = false,
                });
        }

        [TestMethod]
        public async Task EditCalculation_GivenUserEditsAdditionalCalculation_ThenRestrictedFieldsAreNotUpdatedFromRequest()
        {
            //Arrange
            CalculationEditModel calculationEditModel = CreateCalculationEditModel();
            calculationEditModel.DataType = CalculationDataType.String; // Try changing the data type to string

            Reference author = new Reference();

            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.Current.CalculationType = CalculationType.Additional;
            calculation.Current.DataType = CalculationDataType.Decimal;
            calculation.Current.ValueType = CalculationValueType.Number;
            calculation.Current.AllowedEnumTypeValues = null;
            calculation.Current.Namespace = CalculationNamespace.Additional;

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            Mock<IVersionRepository<CalculationVersion>> versionRepository = new Mock<IVersionRepository<CalculationVersion>>();

            CalculationVersion createdCalculationVersion = null;

            versionRepository
                .Setup(m => m.CreateVersion(It.IsAny<CalculationVersion>(), It.IsAny<CalculationVersion>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Callback((CalculationVersion newVersion, CalculationVersion currentVersion, string partitionKey, bool incrementFromCurrentVersion) =>
                {
                    createdCalculationVersion = newVersion;
                })
                .ReturnsAsync(calculationVersion);


            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository.Object,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

            //Act
            IActionResult result = await service.EditCalculation(
                SpecificationId,
                CalculationId,
                calculationEditModel,
                author,
                CorrelationId,
                setAdditional: false,
                calculationEditMode: CalculationEditMode.User,
                existingCalculation: calculation);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            createdCalculationVersion
                .Should()
                .BeEquivalentTo(new CalculationVersion()
                {
                    AllowedEnumTypeValues = null,
                    Author = author,
                    CalculationId = CalculationId,
                    CalculationType = CalculationType.Additional,
                    Comment = null,
                    DataType = CalculationDataType.Decimal,
                    Description = "test description",
                    Name = CalculationName,
                    Namespace = CalculationNamespace.Additional,
                    PublishStatus = PublishStatus.Updated,
                    SourceCode = DefaultSourceCode,
                    SourceCodeName = "CalcName1",
                    ValueType = CalculationValueType.Currency,
                    Version = 1,
                    WasTemplateCalculation = false,
                });
        }

        [TestMethod]
        public async Task EditCalculation_GivenUserEditsTemplateCalculation_ThenRestrictedFieldsAreNotUpdatedFromRequest()
        {
            //Arrange
            CalculationEditModel calculationEditModel = CreateCalculationEditModel();
            calculationEditModel.DataType = CalculationDataType.String; // Try changing the data type to string
            calculationEditModel.Name = "Attempted name change"; // Try changing the name of the calculation
            calculationEditModel.Description = "Attempted description change";

            Reference author = new Reference();

            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.Current.CalculationType = CalculationType.Template;
            calculation.Current.DataType = CalculationDataType.Decimal;
            calculation.Current.ValueType = CalculationValueType.Number;
            calculation.Current.AllowedEnumTypeValues = null;
            calculation.Current.Namespace = CalculationNamespace.Template;
            calculation.Current.Description = "test description";

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            Mock<IVersionRepository<CalculationVersion>> versionRepository = new Mock<IVersionRepository<CalculationVersion>>();

            CalculationVersion createdCalculationVersion = null;

            versionRepository
                .Setup(m => m.CreateVersion(It.IsAny<CalculationVersion>(), It.IsAny<CalculationVersion>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Callback((CalculationVersion newVersion, CalculationVersion currentVersion, string partitionKey, bool incrementFromCurrentVersion) =>
                {
                    createdCalculationVersion = newVersion;
                })
                .ReturnsAsync(calculationVersion);


            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository.Object,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

            //Act
            IActionResult result = await service.EditCalculation(
                SpecificationId,
                CalculationId,
                calculationEditModel,
                author,
                CorrelationId,
                setAdditional: false,
                calculationEditMode: CalculationEditMode.User,
                existingCalculation: calculation);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            createdCalculationVersion
                .Should()
                .BeEquivalentTo(new CalculationVersion()
                {
                    AllowedEnumTypeValues = null,
                    Author = author,
                    CalculationId = CalculationId,
                    CalculationType = CalculationType.Template,
                    Comment = null,
                    DataType = CalculationDataType.Decimal,
                    Description = null, // This field is not stored in cosmos
                    Name = "Test Calc Name",
                    Namespace = CalculationNamespace.Template,
                    PublishStatus = PublishStatus.Updated,
                    SourceCode = DefaultSourceCode,
                    SourceCodeName = "TestCalcName",
                    ValueType = CalculationValueType.Currency,
                    Version = 1,
                    WasTemplateCalculation = false,
                });
        }

        [TestMethod]
        public async Task EditCalculation_GivenSystemEditsTemplateCalculation_ThenAbleToEditRestrictedFields()
        {
            //Arrange
            CalculationEditModel calculationEditModel = CreateCalculationEditModel();
            calculationEditModel.DataType = CalculationDataType.Enum; // Try changing the data type to string
            calculationEditModel.ValueType = CalculationValueType.String; // Try changing the data type to string

            Reference author = new Reference();

            string buildProjectId = Guid.NewGuid().ToString();

            BuildProject buildProject = new BuildProject
            {
                Id = buildProjectId,
            };

            Calculation calculation = CreateCalculation();
            calculation.Current.CalculationType = CalculationType.Template;
            calculation.Current.DataType = CalculationDataType.Decimal;
            calculation.Current.ValueType = CalculationValueType.Number;
            calculation.Current.AllowedEnumTypeValues = null;
            calculation.Current.Namespace = CalculationNamespace.Template;

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

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = calculation.SpecificationId,
                Name = "Test Spec Name",
                FundingStreams = new[]
                {
                    new Reference(calculation.FundingStreamId, "funding stream name")
                }
            };

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .GetSpecificationSummaryById(Arg.Is(calculation.SpecificationId))
                .Returns(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            CalculationVersion calculationVersion = calculation.Current as CalculationVersion;
            calculationVersion.PublishStatus = PublishStatus.Updated;

            Mock<IVersionRepository<CalculationVersion>> versionRepository = new Mock<IVersionRepository<CalculationVersion>>();

            CalculationVersion createdCalculationVersion = null;

            versionRepository
                .Setup(m => m.CreateVersion(It.IsAny<CalculationVersion>(), It.IsAny<CalculationVersion>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Callback((CalculationVersion newVersion, CalculationVersion currentVersion, string partitionKey, bool incrementFromCurrentVersion) =>
                {
                    createdCalculationVersion = newVersion;
                })
                .ReturnsAsync(calculationVersion);


            Build build = new Build();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>())
                .Returns(build);

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1" });

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                buildProjectsService: buildProjectsService,
                searchRepository: searchRepository,
                specificationsApiClient: specificationsApiClient,
                calculationVersionRepository: versionRepository.Object,
                sourceCodeService: sourceCodeService,
                jobManagement: jobManagement);

            //Act
            IActionResult result = await service.EditCalculation(
                SpecificationId,
                CalculationId,
                calculationEditModel,
                author,
                CorrelationId,
                setAdditional: false,
                calculationEditMode: CalculationEditMode.System,
                existingCalculation: calculation);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            createdCalculationVersion
                .Should()
                .BeEquivalentTo(new CalculationVersion()
                {
                    AllowedEnumTypeValues = null,
                    Author = author,
                    CalculationId = CalculationId,
                    CalculationType = CalculationType.Template,
                    Comment = null,
                    DataType = CalculationDataType.Enum,
                    Description = "test description",
                    Name = CalculationName,
                    Namespace = CalculationNamespace.Template,
                    PublishStatus = PublishStatus.Updated,
                    SourceCode = DefaultSourceCode,
                    SourceCodeName = "CalcName1",
                    ValueType = CalculationValueType.String,
                    Version = 1,
                    WasTemplateCalculation = false,
                });
        }
    }
}
