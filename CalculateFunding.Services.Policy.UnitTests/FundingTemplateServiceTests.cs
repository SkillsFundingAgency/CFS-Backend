using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.MappingProfiles;
using CalculateFunding.Services.Policy.Models;
using CalculateFunding.Services.Policy.UnitTests;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using TemplateMetadataSchema10 = CalculateFunding.Common.TemplateMetadata.Schema10;

namespace CalculateFunding.Services.Policy
{
    [TestClass]
    public class FundingTemplateServiceTests
    {
        private const string createdAtActionName = "GetFundingTemplate";
        private const string createdAtControllerName = "Schema";

        [DataRow(true)]
        [DataRow(false)]
        [TestMethod]
        public async Task TemplateExists_ChecksBlobExistsForFundingStreamIdAndVersionSupplied(bool expectedExistsFlag)
        {
            string fundingStreamId = NewRandomString();
            string templateVersion = NewRandomString();
            string fundingPeriodId = NewRandomString();

            IFundingTemplateRepository fundingTemplateRepository = Substitute.For<IFundingTemplateRepository>();
            FundingTemplateService service = CreateFundingTemplateService(fundingTemplateRepository: fundingTemplateRepository);

            fundingTemplateRepository.TemplateVersionExists($"{fundingStreamId}/{fundingPeriodId}/{templateVersion}.json")
                .Returns(expectedExistsFlag);

            bool templateExists = await service.TemplateExists(fundingStreamId, fundingPeriodId, templateVersion);

            templateExists
                .Should()
                .Be(expectedExistsFlag);
        }

        private string NewRandomString() => new RandomString();

        [DataRow("")]
        [DataRow("     ")]
        [TestMethod]
        public async Task SaveFundingTemplate_GivenEmptyTemplate_ReturnsBadRequest(string template)
        {
            //Arrange
            string fundingStreamId = NewRandomString();
            string templateVersion = NewRandomString();
            string fundingPeriodId = NewRandomString();
            FundingTemplateService fundingTemplateService = CreateFundingTemplateService();

            //Act
            IActionResult result = await fundingTemplateService.SaveFundingTemplate(createdAtActionName, createdAtControllerName, template, fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding template was provided.");
        }

        [TestMethod]
        public async Task SaveFundingTemplate_GivenTemplateButDidNotValidate_ReturnsBadRequest()
        {
            //Arrange
            const string template = "a template";
            string fundingStreamId = NewRandomString();
            string templateVersion = NewRandomString();
            string fundingPeriodId = NewRandomString();

            FundingTemplateValidationResult validationResult = new FundingTemplateValidationResult();
            validationResult.Errors.Add(new ValidationFailure("prop1", "an error"));
            validationResult.Errors.Add(new ValidationFailure("prop2", "another error"));

            IFundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();
            fundingTemplateValidationService
                .ValidateFundingTemplate(Arg.Is(template), Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId), Arg.Is(templateVersion))
                .Returns(validationResult);

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(fundingTemplateValidationService: fundingTemplateValidationService);

            //Act
            IActionResult result = await fundingTemplateService.SaveFundingTemplate(createdAtActionName, createdAtControllerName, template, fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObjectResult = result as BadRequestObjectResult;

            badRequestObjectResult
                .Value
                .Should()
                .BeOfType<SerializableError>();

            SerializableError modelState = badRequestObjectResult.Value as SerializableError;

            modelState
                .Should()
                .NotBeNull();

            modelState
                .Values
                .Should()
                .HaveCount(2);
        }

        [TestMethod]
        public async Task SaveFundingTemplate_GivenValidTemplateButFailedToSaveToBlobStorage_ReturnsInternalServerError()
        {
            //Arrange
            const string template = "a template";
            string fundingStreamId = NewRandomString();
            string templateVersion = NewRandomString();
            string fundingPeriodId = NewRandomString();

            FundingTemplateValidationResult validationResult = new FundingTemplateValidationResult
            {
                TemplateVersion = "1.8",
                FundingStreamId = "PES",
                SchemaVersion = "1.0",
                FundingPeriodId = "AY-2020"
            };

            string blobName = $"{validationResult.FundingStreamId}/{validationResult.FundingPeriodId}/{validationResult.TemplateVersion}.json";

            IFundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();
            fundingTemplateValidationService
                .ValidateFundingTemplate(Arg.Is(template), Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId), Arg.Is(templateVersion))
                .Returns(validationResult);

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();
            fundingTemplateRepository
                .When(x => x.SaveFundingTemplateVersion(Arg.Is(blobName), Arg.Any<byte[]>()))
                .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetadataGenerator = CreateMetadataGenerator();
            templateMetadataGenerator.Validate(Arg.Is<string>(template))
                .Returns(new FluentValidation.Results.ValidationResult());

            ITemplateMetadataResolver templateMetadataResolver = CreateMetadataResolver("1.0", templateMetadataGenerator);

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                logger,
                fundingTemplateValidationService: fundingTemplateValidationService,
                fundingTemplateRepository: fundingTemplateRepository,
                templateMetadataResolver: templateMetadataResolver);

            //Act
            IActionResult result = await fundingTemplateService.SaveFundingTemplate(createdAtActionName, createdAtControllerName, template, fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Error occurred uploading funding template");

            logger
                .Received(1)
                .Error(Arg.Any<NonRetriableException>(), Arg.Is($"Failed to save funding template '{blobName}' to blob storage"));
        }

        [TestMethod]
        public async Task SaveFundingTemplate_GivenValidTemplateAndSaves_InvalidatesCacheReturnsCreatedAtActionResult()
        {
            //Arrange
            string template = CreateJsonFile("CalculateFunding.Services.Policy.Resources.LogicalModelTemplateNoProfilePeriods.json");
            string fundingStreamId = "PES";
            string templateVersion = "1.5";
            string fundingPeriodId = "AY-2020";

            FundingTemplateValidationResult validationResult = new FundingTemplateValidationResult
            {
                TemplateVersion = templateVersion,
                FundingStreamId = fundingStreamId,
                SchemaVersion = "1.0",
                FundingPeriodId = fundingPeriodId
            };

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{validationResult.FundingStreamId}-{validationResult.FundingPeriodId}-{validationResult.TemplateVersion}".ToLowerInvariant();

            ITemplateMetadataResolver templateMetadataResolver = CreateMetadataResolver("1.0");

            IFundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();
            fundingTemplateValidationService
                .ValidateFundingTemplate(Arg.Is(template), Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId), Arg.Is(templateVersion))
                .Returns(validationResult);

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                logger,
                fundingTemplateValidationService: fundingTemplateValidationService,
                fundingTemplateRepository: fundingTemplateRepository,
                cacheProvider: cacheProvider,
                templateMetadataResolver: templateMetadataResolver);

            //Act
            IActionResult result = await fundingTemplateService.SaveFundingTemplate(createdAtActionName, createdAtControllerName, template, fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                 .Should()
                 .BeAssignableTo<CreatedAtActionResult>();

            CreatedAtActionResult actionResult = result as CreatedAtActionResult;

            actionResult
                .ActionName
                .Should()
                .Be(createdAtActionName);

            actionResult
               .ControllerName
               .Should()
               .Be(createdAtControllerName);

            actionResult
               .RouteValues["fundingStreamId"].ToString()
               .Should()
               .Be("PES");

            actionResult
               .RouteValues["fundingPeriodId"].ToString()
               .Should()
               .Be("AY-2020");

            actionResult
                .RouteValues["templateVersion"].ToString()
                .Should()
                .Be("1.5");

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<string>(Arg.Is(cacheKey));

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<FundingTemplateContents>(Arg.Is($"{CacheKeys.FundingTemplateContents}pes:ay-2020:1.5"));

            await cacheProvider
                .Received(1)
                .RemoveAsync<TemplateMetadataContents>($"{CacheKeys.FundingTemplateContentMetadata}pes:ay-2020:1.5");

            await cacheProvider
                .Received(1)
                .RemoveAsync<TemplateMetadataDistinctContents>($"{CacheKeys.FundingTemplateContentMetadataDistinct}pes:ay-2020:1.5");
        }

        [TestMethod]
        public async Task SaveFundingTemplate_GivenInvalidTemplateDueToProfilePeriods_BadRequest()
        {
            //Arrange
            string template = CreateJsonFile("CalculateFunding.Services.Policy.Resources.LogicalModelTemplate.json");
            string fundingStreamId = "PES";
            string templateVersion = "1.5";
            string fundingPeriodId = "AY-2020";

            FundingTemplateValidationResult validationResult = new FundingTemplateValidationResult
            {
                TemplateVersion = templateVersion,
                FundingStreamId = fundingStreamId,
                SchemaVersion = "1.0",
            };

            ITemplateMetadataResolver templateMetadataResolver = CreateMetadataResolver("1.0");

            IFundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();
            fundingTemplateValidationService
                .ValidateFundingTemplate(Arg.Is(template), Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId), Arg.Is(templateVersion))
                .Returns(validationResult);

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                logger,
                fundingTemplateValidationService: fundingTemplateValidationService,
                fundingTemplateRepository: fundingTemplateRepository,
                cacheProvider: cacheProvider,
                templateMetadataResolver: templateMetadataResolver);

            //Act
            IActionResult result = await fundingTemplateService.SaveFundingTemplate(createdAtActionName, createdAtControllerName, template, fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                 .Should()
                 .BeAssignableTo<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObjectResult = result as BadRequestObjectResult;

            SerializableError validationResults = badRequestObjectResult.Value as SerializableError;

            validationResults
                .Count()
                .Should()
                .Be(1);

            ((string[])validationResults["DistributionPeriods"])[0]
                .Should()
                .Be("Funding line : 'Total funding line' has values for the distribution periods");
        }

        [TestMethod]
        public async Task GetFundingTemplate_GivenTemplateIsInCache_ReturnsOkObjectResultFromCache()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string templateVersion = "1.2";
            const string fundingPeriodId = "AY-2020";

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{fundingPeriodId}-{templateVersion}";

            string template = "a template";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKey))
                .Returns(template);

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(cacheProvider: cacheProvider);

            //Act
            IActionResult result = await fundingTemplateService.GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(template);
        }

        [TestMethod]
        public async Task GetFundingTemplate_GivenTemplateDoesNotExistInBlobStorage_ReturnsNotFoundResult()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string templateVersion = "1.2";
            const string fundingPeriodId = "AY-2020";

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{fundingPeriodId}-{templateVersion}";

            string blobName = $"{fundingStreamId}/{fundingPeriodId}/{templateVersion}.json";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKey))
                .Returns((string)null);

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();
            fundingTemplateRepository
                .TemplateVersionExists(Arg.Is(blobName))
                .Returns(false);

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                cacheProvider: cacheProvider,
                fundingTemplateRepository: fundingTemplateRepository);

            //Act
            IActionResult result = await fundingTemplateService.GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetFundingTemplate_GivenTemplateDoesExistInBlobStorageButIsEmpty_ReturnsInternalServerError()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string templateVersion = "1.2";
            const string fundingPeriodId = "AY-2020";

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{fundingPeriodId}-{templateVersion}";

            string blobName = $"{fundingStreamId}/{fundingPeriodId}/{templateVersion}.json";

            string template = string.Empty;

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKey))
                .Returns((string)null);

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();

            fundingTemplateRepository
                .TemplateVersionExists(Arg.Is(blobName))
                .Returns(true);

            fundingTemplateRepository
                .GetFundingTemplateVersion(Arg.Is(blobName))
                .Returns(template);

            ILogger logger = CreateLogger();

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                logger,
                cacheProvider: cacheProvider,
                fundingTemplateRepository: fundingTemplateRepository);

            //Act
            IActionResult result = await fundingTemplateService.GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to retreive blob contents for funding stream id '{fundingStreamId}', funding period id '{fundingPeriodId}' and funding template version '{templateVersion}'");

            logger
                .Received(1)
                .Error($"Empty template returned from blob storage for blob name '{blobName}'");
        }

        [TestMethod]
        public async Task GetFundingTemplate_GivenCheckingTemplateExistsFails_ReturnsInternalServerError()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string templateVersion = "1.2";
            const string fundingPeriodId = "AY-2020";

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{fundingPeriodId}-{templateVersion}";

            string blobName = $"{fundingStreamId}/{fundingPeriodId}/{templateVersion}.json";

            string template = string.Empty;

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKey))
                .Returns((string)null);

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();

            fundingTemplateRepository
               .When(x => x.TemplateVersionExists(Arg.Is(blobName)))
               .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                logger,
                cacheProvider: cacheProvider,
                fundingTemplateRepository: fundingTemplateRepository);

            //Act
            IActionResult result = await fundingTemplateService.GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Error occurred fetching funding template for funding stream id '{fundingStreamId}', funding period id '{fundingPeriodId}' and version '{templateVersion}'");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), $"Failed to fetch funding template '{blobName}' from blob storage");
        }

        [TestMethod]
        public async Task GetFundingTemplate_GivenFetechingBlobFails_ReturnsInternalServerError()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string templateVersion = "1.2";
            const string fundingPeriodId = "AY-2020";

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{fundingPeriodId}-{templateVersion}";

            string blobName = $"{fundingStreamId}/{fundingPeriodId}/{templateVersion}.json";

            string template = string.Empty;

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKey))
                .Returns((string)null);

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();

            fundingTemplateRepository
               .TemplateVersionExists(Arg.Is(blobName))
               .Returns(true);

            fundingTemplateRepository
               .When(x => x.GetFundingTemplateVersion(Arg.Is(blobName)))
               .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                logger,
                cacheProvider: cacheProvider,
                fundingTemplateRepository: fundingTemplateRepository);

            //Act
            IActionResult result = await fundingTemplateService.GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Error occurred fetching funding template for funding stream id '{fundingStreamId}', funding period id '{fundingPeriodId}' and version '{templateVersion}'");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), $"Failed to fetch funding template '{blobName}' from blob storage");
        }

        [TestMethod]
        public async Task GetFundingTemplate_GivenFetechingBlobSucceeds_SetsCacheAndReturnsOKObjectResult()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string templateVersion = "1.2";
            const string fundingPeriodId = "AY-2020";

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{fundingPeriodId}-{templateVersion}";

            string blobName = $"{fundingStreamId}/{fundingPeriodId}/{templateVersion}.json";

            string template = "a template";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKey))
                .Returns((string)null);

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();

            fundingTemplateRepository
               .TemplateVersionExists(Arg.Is(blobName))
               .Returns(true);

            fundingTemplateRepository
              .GetFundingTemplateVersion(Arg.Is(blobName))
              .Returns(template);

            ILogger logger = CreateLogger();

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                logger,
                cacheProvider: cacheProvider,
                fundingTemplateRepository: fundingTemplateRepository);

            //Act
            IActionResult result = await fundingTemplateService.GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(template);

            await
                cacheProvider
                    .Received(1)
                    .SetAsync(Arg.Is(cacheKey), Arg.Is(template));
        }

        [TestMethod]
        public async Task GetFundingTemplates_GivenTemplateIsInBlob_ReturnsOkObjectResult()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string fundingPeriodId = "AY-2020";

            string blobNamePrefix = $"{fundingStreamId}/{ fundingPeriodId}/";

            IEnumerable<TemplateSummaryResponse> templateVersions = new[]
            {
                new TemplateSummaryResponse()
                {
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId,
                    MajorVersion = 1,
                    MinorVersion = 0,
                    Status = TemplateStatus.Published
                }
            };
            IEnumerable<PublishedFundingTemplate> pubblishedFundingTempalte = new[]
            {
                new PublishedFundingTemplate()
                {
                    TemplateVersion = "1.0"
                }

            };

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();

            fundingTemplateRepository
                .SearchTemplates(Arg.Is(blobNamePrefix))
                .Returns(pubblishedFundingTempalte);

            ITemplateBuilderService templateBuilderService = CreateTemplateBuilderService();
            templateBuilderService.FindVersionsByFundingStreamAndPeriod(Arg.Is<FindTemplateVersionQuery>(x => x.FundingStreamId == fundingStreamId && x.FundingPeriodId == fundingPeriodId))
                                .Returns(templateVersions);


            ILogger logger = CreateLogger();

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                logger,
                fundingTemplateRepository: fundingTemplateRepository,
                templateBuilderService: templateBuilderService);

            //Act
            IActionResult result = await fundingTemplateService.GetFundingTemplates(fundingStreamId, fundingPeriodId);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(pubblishedFundingTempalte);
        }

        [TestMethod]
        public async Task GetFundingTemplates_GivenTemplateIsInBlob_Metatdata_In_TemplateBuilder_Collection_ReturnsOkObjectResultWithAMetadata()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string fundingPeriodId = "AY-2020";

            string blobNamePrefix = $"{fundingStreamId}/{ fundingPeriodId}/";

            IEnumerable<TemplateSummaryResponse> templateVersions = new[]
            {
                new TemplateSummaryResponse()
                {
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId,
                    MajorVersion = 1,
                    MinorVersion = 1,
                    Status = TemplateStatus.Published,
                    AuthorId = "Auth-Id",
                    AuthorName = "Auth-Name",
                    Comments = "SomeComments",
                    SchemaVersion = "1.1"
                },
                new TemplateSummaryResponse()
                {
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId,
                    MajorVersion = 1,
                    MinorVersion = 0,
                    Status = TemplateStatus.Published,
                    AuthorId = "Auth-Id2",
                    AuthorName = "Auth-Name2",
                    Comments = "SomeComments2",
                    SchemaVersion = "1.2"
                }
            };

            IEnumerable<PublishedFundingTemplate> pubblishedFundingTempaltes = new[]
            {
                new PublishedFundingTemplate()
                {
                    TemplateVersion = "1.0",
                    PublishDate = new DateTime(2020, 06, 15, 10, 30, 50)
                }

            };

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();

            fundingTemplateRepository
                .SearchTemplates(Arg.Is(blobNamePrefix))
                .Returns(pubblishedFundingTempaltes);

            ITemplateBuilderService templateBuilderService = CreateTemplateBuilderService();
            templateBuilderService.FindVersionsByFundingStreamAndPeriod(Arg.Is<FindTemplateVersionQuery>(x => x.FundingStreamId == fundingStreamId && x.FundingPeriodId == fundingPeriodId))
                                .Returns(templateVersions);


            ILogger logger = CreateLogger();

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                logger,
                fundingTemplateRepository: fundingTemplateRepository,
                templateBuilderService: templateBuilderService);

            //Act
            IActionResult result = await fundingTemplateService.GetFundingTemplates(fundingStreamId, fundingPeriodId);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>();
            PublishedFundingTemplate resultTemplate = ((OkObjectResult)result).Value.As<IEnumerable<PublishedFundingTemplate>>().FirstOrDefault();
            TemplateSummaryResponse templateResponse = templateVersions.First(x => x.MajorVersion == 1 && x.MinorVersion == 0);

            resultTemplate.TemplateVersion.Should().Be(pubblishedFundingTempaltes.First().TemplateVersion);
            resultTemplate.PublishDate.Should().Be(pubblishedFundingTempaltes.First().PublishDate);
            resultTemplate.AuthorId.Should().Be(templateResponse.AuthorId);
            resultTemplate.AuthorName.Should().Be(templateResponse.AuthorName);
            resultTemplate.PublishNote.Should().Be(templateResponse.Comments);
            resultTemplate.SchemaVersion.Should().Be(templateResponse.SchemaVersion);
        }

        [TestMethod]
        public async Task GetFundingTemplates_GivenNoTemplateIsInBlob_ReturnsNotFound()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string fundingPeriodId = "AY-2020";

            string blobNamePrefix = $"{fundingStreamId}/{ fundingPeriodId}/";

            IEnumerable<TemplateSummaryResponse> templateVersions = new[]
            {
                new TemplateSummaryResponse()
                {
                    FundingPeriodId = fundingPeriodId,
                    FundingStreamId = fundingStreamId,
                    MajorVersion = 1,
                    MinorVersion = 0,
                    Status = TemplateStatus.Published
                }
            };
            IEnumerable<PublishedFundingTemplate> pubblishedFundingTempalte = Enumerable.Empty<PublishedFundingTemplate>();

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();

            fundingTemplateRepository
                .SearchTemplates(Arg.Is(blobNamePrefix))
                .Returns(pubblishedFundingTempalte);

            ITemplateBuilderService templateBuilderService = CreateTemplateBuilderService();
            templateBuilderService.FindVersionsByFundingStreamAndPeriod(Arg.Is<FindTemplateVersionQuery>(x => x.FundingStreamId == fundingStreamId && x.FundingPeriodId == fundingPeriodId))
                                .Returns(templateVersions);


            ILogger logger = CreateLogger();

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
                logger,
                fundingTemplateRepository: fundingTemplateRepository,
                templateBuilderService: templateBuilderService);

            //Act
            IActionResult result = await fundingTemplateService.GetFundingTemplates(fundingStreamId, fundingPeriodId);

            //Assert
            result
                .Should()
                .BeAssignableTo<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetDistinctFundingTemplateMetadataFundingLinesContents_GivenTemplateIsInBlob_ReturnsOkObjecResultstWithDistinctFundingLinesAndCalculations()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string fundingPeriodId = "AY-2020";
            const string templateVersion = "1.0";

            string template = CreateJsonFile("CalculateFunding.Services.Policy.Resources.LogicalModelTemplateForDistinctMetadataContents.json");

            string blobNamePrefix = $"{fundingStreamId}/{ fundingPeriodId}/{templateVersion}";

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();
            fundingTemplateRepository
                .TemplateVersionExists(Arg.Is<string>(_ => _.Contains(blobNamePrefix)))
                .Returns(true);
            fundingTemplateRepository.GetFundingTemplateVersion(Arg.Is<string>(_ => _.Contains(blobNamePrefix)))
                .Returns(template);

            ILogger logger = CreateLogger();
            ICacheProvider cacheProvider = CreateCacheProvider();

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
               logger,
               fundingTemplateRepository: fundingTemplateRepository,
               cacheProvider: cacheProvider);

            //Act
            IActionResult result = await fundingTemplateService.GetDistinctFundingTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>();

            TemplateMetadataDistinctContents contents = ((OkObjectResult)result).Value.As<TemplateMetadataDistinctContents>();
            contents.Should().NotBeNull();
            contents.FundingLines.Count().Should().Be(2);
            contents.Calculations.Count().Should().Be(2);
            contents.FundingStreamId.Should().Be(fundingStreamId);
            contents.FundingPeriodId.Should().Be(fundingPeriodId);
            contents.TemplateVersion.Should().Be(templateVersion);

            string cacheKey = $"{CacheKeys.FundingTemplateContentMetadataDistinct}{fundingStreamId}:{fundingPeriodId}:{templateVersion}".ToLowerInvariant();
            await cacheProvider
                .Received(1)
                .SetAsync(cacheKey,
                Arg.Is<TemplateMetadataDistinctContents>(_ => _.FundingStreamId == fundingStreamId && _.FundingPeriodId == fundingPeriodId && _.TemplateVersion == templateVersion),
                TimeSpan.FromDays(365),
                true,
                null);
        }

        [TestMethod]
        public async Task GetDistinctFundingTemplateMetadataFundingLinesContents_GivenContentsInCache_ReturnsOkObjecWithResultsFromCache()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string fundingPeriodId = "AY-2020";
            const string templateVersion = "1.0";
            string cacheKey = $"{CacheKeys.FundingTemplateContentMetadataDistinct}{fundingStreamId}:{fundingPeriodId}:{templateVersion}".ToLowerInvariant();

            string blobNamePrefix = $"{fundingStreamId}/{ fundingPeriodId}/{templateVersion}";

            IFundingTemplateRepository fundingTemplateRepository = CreateFundingTemplateRepository();
            fundingTemplateRepository
                .TemplateVersionExists(Arg.Is<string>(_ => _.Contains(blobNamePrefix)))
                .Returns(true);
            fundingTemplateRepository.GetFundingTemplateVersion(Arg.Is<string>(_ => _.Contains(blobNamePrefix)))
                .Returns(string.Empty);

            ILogger logger = CreateLogger();
            ICacheProvider cacheProvider = CreateCacheProvider();

            cacheProvider.GetAsync<TemplateMetadataDistinctContents>(cacheKey)
                .Returns(new TemplateMetadataDistinctContents() { 
                    FundingStreamId = fundingStreamId, 
                    FundingPeriodId = fundingPeriodId, 
                    TemplateVersion = templateVersion});

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(
               logger,
               fundingTemplateRepository: fundingTemplateRepository,
               cacheProvider: cacheProvider);

            //Act
            IActionResult result = await fundingTemplateService.GetDistinctFundingTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>();

            TemplateMetadataDistinctContents contents = ((OkObjectResult)result).Value.As<TemplateMetadataDistinctContents>();
            contents.Should().NotBeNull();
            contents.FundingStreamId.Should().Be(fundingStreamId);
            contents.FundingPeriodId.Should().Be(fundingPeriodId);
            contents.TemplateVersion.Should().Be(templateVersion);

            await cacheProvider
                .Received(1).GetAsync<TemplateMetadataDistinctContents>(cacheKey);

            await fundingTemplateRepository
                .Received(0).GetFundingTemplateVersion(Arg.Is<string>(_ => _.Contains(blobNamePrefix)));

            await cacheProvider
                .Received(0)
                .SetAsync(cacheKey,
                Arg.Is<TemplateMetadataDistinctContents>(_ => _.FundingStreamId == fundingStreamId && _.FundingPeriodId == fundingPeriodId && _.TemplateVersion == templateVersion),
                TimeSpan.FromDays(365),
                true,
                null);
        }

        private static FundingTemplateService CreateFundingTemplateService(
            ILogger logger = null,
            IFundingTemplateRepository fundingTemplateRepository = null,
            IFundingTemplateValidationService fundingTemplateValidationService = null,
            ICacheProvider cacheProvider = null,
            ITemplateMetadataResolver templateMetadataResolver = null,
            ITemplateBuilderService templateBuilderService = null,
            IMapper mapper = null)
        {
            if(mapper == null)
            {
                MapperConfiguration fundingConfMappingConfig = new MapperConfiguration(c =>
                {
                    c.AddProfile<FundingConfigurationMappingProfile>();
                });

                mapper = fundingConfMappingConfig.CreateMapper();
            }

            return new FundingTemplateService(
                   logger ?? CreateLogger(),
                   fundingTemplateRepository ?? CreateFundingTemplateRepository(),
                   PolicyResiliencePoliciesTestHelper.GenerateTestPolicies(),
                   fundingTemplateValidationService ?? CreateFundingTemplateValidationService(),
                   cacheProvider ?? CreateCacheProvider(),
                   templateMetadataResolver ?? CreateMetadataResolver(),
                   templateBuilderService ?? CreateTemplateBuilderService(),
                   mapper
                );
        }

        private static ITemplateMetadataResolver CreateMetadataResolver(string schemaVersion = "1.0", ITemplateMetadataGenerator tempateMetadataGenerator = null)
        {
            TemplateMetadataResolver resolver = new TemplateMetadataResolver();
            switch (schemaVersion)
            {
                case "1.0":
                    {
                        resolver.Register(schemaVersion, tempateMetadataGenerator ?? new TemplateMetadataSchema10.TemplateMetadataGenerator(CreateLogger()));
                        break;
                    }
            }

            return resolver;
        }

        private static string CreateJsonFile(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        private static ITemplateMetadataGenerator CreateMetadataGenerator()
        {
            return Substitute.For<ITemplateMetadataGenerator>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IFundingTemplateRepository CreateFundingTemplateRepository()
        {
            return Substitute.For<IFundingTemplateRepository>();
        }

        private static ITemplateBuilderService CreateTemplateBuilderService()
        {
            return Substitute.For<ITemplateBuilderService>();
        }

        private static IFundingTemplateValidationService CreateFundingTemplateValidationService()
        {
            return Substitute.For<IFundingTemplateValidationService>();
        }

        private static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }
    }
}
