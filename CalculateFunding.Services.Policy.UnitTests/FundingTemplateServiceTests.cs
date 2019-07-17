using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using CalculateFunding.Services.Policy.UnitTests;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        [DataRow("")]
        [DataRow("     ")]
        [TestMethod]
        public async Task SaveFundingTemplate_GivenEmptyTemplate_ReturnsBadRequest(string template)
        {
            //Arrange
            byte[] byteArray = Encoding.UTF8.GetBytes(template);

            MemoryStream stream = new MemoryStream(byteArray);
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService();

            //Act
            IActionResult result = await fundingTemplateService.SaveFundingTemplate(createdAtActionName, createdAtControllerName, request);

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

            byte[] byteArray = Encoding.UTF8.GetBytes(template);

            MemoryStream stream = new MemoryStream(byteArray);
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            FundingTemplateValidationResult validationResult = new FundingTemplateValidationResult();
            validationResult.ValidationState.Errors.Add("an error");
            validationResult.ValidationState.Errors.Add("another error");

            IFundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();
            fundingTemplateValidationService
                .ValidateFundingTemplate(Arg.Is(template))
                .Returns(validationResult);

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(fundingTemplateValidationService: fundingTemplateValidationService);

            //Act
            IActionResult result = await fundingTemplateService.SaveFundingTemplate(createdAtActionName, createdAtControllerName, request);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObjectResult = result as BadRequestObjectResult;

            ModelState validationState = badRequestObjectResult.Value as ModelState;

            validationState
                .Errors
                .Should()
                .HaveCount(2);
        }

        [TestMethod]
        public async Task SaveFundingTemplate_GivenValidTemplateButFailedToSaveToBlobStorage_ReturnsInternalServerError()
        {
            //Arrange
            const string template = "a template";

            byte[] byteArray = Encoding.UTF8.GetBytes(template);

            MemoryStream stream = new MemoryStream(byteArray);
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            FundingTemplateValidationResult validationResult = new FundingTemplateValidationResult
            {
                Version = "1.0",
                FundingStreamId = "PES"
            };

            string blobName = $"{validationResult.FundingStreamId}/{validationResult.Version}.json";

            IFundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();
            fundingTemplateValidationService
                .ValidateFundingTemplate(Arg.Is(template))
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
            IActionResult result = await fundingTemplateService.SaveFundingTemplate(createdAtActionName, createdAtControllerName, request);

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
        public async Task SaveFundingTemplate_GivenValidTemplateAndSaves_UpdatesCacheReturnsCreatedAtActionResult()
        {
            //Arrange
            string template = CreateJsonFile("CalculateFunding.Services.Policy.Resources.LogicalModelTemplateNoProfilePeriods.json");

            byte[] byteArray = Encoding.UTF8.GetBytes(template);

            MemoryStream stream = new MemoryStream(byteArray);
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            FundingTemplateValidationResult validationResult = new FundingTemplateValidationResult
            {
                Version = "1.0",
                FundingStreamId = "PES"
            };

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{validationResult.FundingStreamId}-{validationResult.Version}";

            string blobName = $"{validationResult.FundingStreamId}/{validationResult.Version}.json";

            ITemplateMetadataResolver templateMetadataResolver = CreateMetadataResolver("1.0");

            IFundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();
            fundingTemplateValidationService
                .ValidateFundingTemplate(Arg.Is(template))
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
            IActionResult result = await fundingTemplateService.SaveFundingTemplate(createdAtActionName, createdAtControllerName, request);

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
                .RouteValues["templateVersion"].ToString()
                .Should()
                .Be("1.0");

            await
                cacheProvider
                    .Received(1)
                    .SetAsync(Arg.Is(cacheKey), Arg.Is(template));
        }

        [TestMethod]
        public async Task SaveFundingTemplate_GivenInvalidTemplateDueToProfilePeriods_BadRequest()
        {
            //Arrange
            string template = CreateJsonFile("CalculateFunding.Services.Policy.Resources.LogicalModelTemplate.json");

            byte[] byteArray = Encoding.UTF8.GetBytes(template);

            MemoryStream stream = new MemoryStream(byteArray);
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            FundingTemplateValidationResult validationResult = new FundingTemplateValidationResult
            {
                Version = "1.0",
                FundingStreamId = "PES"
            };

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{validationResult.FundingStreamId}-{validationResult.Version}";

            string blobName = $"{validationResult.FundingStreamId}/{validationResult.Version}.json";

            ITemplateMetadataResolver templateMetadataResolver = CreateMetadataResolver("1.0");

            IFundingTemplateValidationService fundingTemplateValidationService = CreateFundingTemplateValidationService();
            fundingTemplateValidationService
                .ValidateFundingTemplate(Arg.Is(template))
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
            IActionResult result = await fundingTemplateService.SaveFundingTemplate(createdAtActionName, createdAtControllerName, request);

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

            ((string[])validationResults["ProfilePeriods"])[0]
                .Should()
                .Be("Funding line : 'Total funding line' has values for the profilePeriods");
        }

        [TestMethod]
        public async Task GetFundingTemplate_GivenTemplateIsInCache_ReturnsOkObjectResultFromCache()
        {
            //Arrange
            const string fundingStreamId = "PES";
            const string templateVersion = "1.2";

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{templateVersion}";

            string template = "a template";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKey))
                .Returns(template);

            FundingTemplateService fundingTemplateService = CreateFundingTemplateService(cacheProvider: cacheProvider);

            //Act
            IActionResult result = await fundingTemplateService.GetFundingTemplate(fundingStreamId, templateVersion);

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

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{templateVersion}";

            string blobName = $"{fundingStreamId}/{templateVersion}.json";

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
            IActionResult result = await fundingTemplateService.GetFundingTemplate(fundingStreamId, templateVersion);

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

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{templateVersion}";

            string blobName = $"{fundingStreamId}/{templateVersion}.json";

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
            IActionResult result = await fundingTemplateService.GetFundingTemplate(fundingStreamId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to retreive blob contents for funding stream id '{fundingStreamId}' funding template version '{templateVersion}'");

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

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{templateVersion}";

            string blobName = $"{fundingStreamId}/{templateVersion}.json";

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
            IActionResult result = await fundingTemplateService.GetFundingTemplate(fundingStreamId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Error occurred fetching funding template for funding stream id '{fundingStreamId}' and version '{templateVersion}'");

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

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{templateVersion}";

            string blobName = $"{fundingStreamId}/{templateVersion}.json";

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
            IActionResult result = await fundingTemplateService.GetFundingTemplate(fundingStreamId, templateVersion);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Error occurred fetching funding template for funding stream id '{fundingStreamId}' and version '{templateVersion}'");

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

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{templateVersion}";

            string blobName = $"{fundingStreamId}/{templateVersion}.json";

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
            IActionResult result = await fundingTemplateService.GetFundingTemplate(fundingStreamId, templateVersion);

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

        private static FundingTemplateService CreateFundingTemplateService(
            ILogger logger = null,
            IFundingTemplateRepository fundingTemplateRepository = null,
            IFundingTemplateValidationService fundingTemplateValidationService = null,
            ICacheProvider cacheProvider = null,
            ITemplateMetadataResolver templateMetadataResolver = null)
        {
            return new FundingTemplateService(
                   logger ?? CreateLogger(),
                   fundingTemplateRepository ?? CreateFundingTemplateRepository(),
                   PolicyResiliencePoliciesTestHelper.GenerateTestPolicies(),
                   fundingTemplateValidationService ?? CreateFundingTemplateValidationService(),
                   cacheProvider ?? CreateCacheProvider(),
                   templateMetadataResolver ?? CreateMetadataResolver()
                );
        }

        private static ITemplateMetadataResolver CreateMetadataResolver(string schemaVersion = "1.0", ITemplateMetadataGenerator tempateMetadataGenerator = null)
        {
            TemplateMetadataResolver resolver = new TemplateMetadataResolver();
            switch(schemaVersion)
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

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                StreamReader reader = new StreamReader(stream);

                return reader.ReadToEnd();
            }
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
