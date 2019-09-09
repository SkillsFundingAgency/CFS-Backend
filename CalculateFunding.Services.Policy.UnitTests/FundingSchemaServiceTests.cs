using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Policy.UnitTests
{
    [TestClass]
    public class FundingSchemaServiceTests
    {
        private const string createdAtActionName = "GetFundingSchemaByVersion";
        private const string createdAtControllerName = "SchemaController";
        private const string fundingSchemaFolder = "funding";

        [DataRow("")]
        [DataRow("     ")]
        [TestMethod]
        public async Task SaveFundingSchema_WhenEmptySchemaProvided_ReturnsBadRequest(string schema)
        {
            //Arrange
            byte[] byteArray = Encoding.UTF8.GetBytes(schema);

            MemoryStream stream = new MemoryStream(byteArray);
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            FundingSchemaService fundingSchemaService = CreateFundingSchemaService();

            //Act
            IActionResult result = await fundingSchemaService.SaveFundingSchema(createdAtActionName, createdAtControllerName, request);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding schema was provided.");
        }

        [TestMethod]
        public async Task SaveFundingSchema_WhenInvalidSchemaProvided_ReturnsBadRequest()
        {
            //Arrange
            byte[] byteArray = Encoding.UTF8.GetBytes("Blah Blah");

            MemoryStream stream = new MemoryStream(byteArray);
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            FundingSchemaService fundingSchemaService = CreateFundingSchemaService(logger);

            //Act
            IActionResult result = await fundingSchemaService.SaveFundingSchema(createdAtActionName, createdAtControllerName, request);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Failed to parse request body as a valid json schema.");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed to parse request body as a valid json schema."));
        }

        [TestMethod]
        [DataRow("CalculateFunding.Services.Policy.Resources.LogicalModelNoVersion.json")]
        [DataRow("CalculateFunding.Services.Policy.Resources.LogicalModelInvalidVersion.json")]
        public async Task SaveFundingSchema_WhenNoVersionIsSet_ReturnsBadRequest(string resourcePath)
        {
            //arrange 
            byte[] byteArray = CreateSchemaFile(resourcePath);

            MemoryStream stream = new MemoryStream(byteArray);
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            FundingSchemaService fundingSchemaService = CreateFundingSchemaService();

            //Act
            IActionResult result = await fundingSchemaService.SaveFundingSchema(createdAtActionName, createdAtControllerName, request);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("An invalid schema version was provided");
        }

        [TestMethod]
        public async Task SaveFundingSchema_WhenVersionDoesNotExistButSavingToBlobStorageCausesAnError_ReturnsNoContentResult()
        {
            //arrange 
            const string version = "1.0";

            string blobName = $"{fundingSchemaFolder}/{version}.json";

            byte[] byteArray = CreateSchemaFile("CalculateFunding.Services.Policy.Resources.LogicalModel.json");

            MemoryStream stream = new MemoryStream(byteArray);
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();

            fundingSchemaRepository
               .When(x => x.SaveFundingSchemaVersion(Arg.Any<string>(), Arg.Any<byte[]>()))
               .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            FundingSchemaService fundingSchemaService = CreateFundingSchemaService(logger, fundingSchemaRepository: fundingSchemaRepository);

            //Act
            IActionResult result = await fundingSchemaService.SaveFundingSchema(createdAtActionName, createdAtControllerName, request);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Error occurred uploading funding schema");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to save funding schema '{blobName}' to blob storage"));
        }

        [TestMethod]
        public async Task SaveFundingSchema_WhenSavingIsSuccessful_ReturnsCreatedAtResult()
        {
            //arrange 
            const string version = "1.0";

            string blobName = $"{fundingSchemaFolder}/{version}.json";

            byte[] byteArray = CreateSchemaFile("CalculateFunding.Services.Policy.Resources.LogicalModel.json");

            MemoryStream stream = new MemoryStream(byteArray);
            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();

            ILogger logger = CreateLogger();

            FundingSchemaService fundingSchemaService = CreateFundingSchemaService(logger, fundingSchemaRepository: fundingSchemaRepository);

            //Act
            IActionResult result = await fundingSchemaService.SaveFundingSchema(createdAtActionName, createdAtControllerName, request);

            //Assert
            result
                .Should()
                .BeAssignableTo<CreatedAtActionResult>();

            CreatedAtActionResult actionResult = result as CreatedAtActionResult;

            actionResult
                .ActionName
                .Should()
                .Be("GetFundingSchemaByVersion");

            actionResult
               .ControllerName
               .Should()
               .Be("SchemaController");

            actionResult
                .RouteValues["schemaVersion"].ToString()
                .Should()
                .Be("1.0");
        }

        [TestMethod]
        public async Task GetFundingSchemaByVersion_GivenASchemaVersionThatDoesNotRxistInBlobStorage_ReturnsNotFound()
        {
            //Arrange
            const string version = "0.6";

            string blobName = $"{fundingSchemaFolder}/{version}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();

            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(false);

            FundingSchemaService fundingSchemaService = CreateFundingSchemaService(fundingSchemaRepository: fundingSchemaRepository);

            //Act
            IActionResult result = await fundingSchemaService.GetFundingSchemaByVersion(version);

            //Assert
            result
                .Should()
                .BeAssignableTo<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetFundingSchemaByVersion_WhenCheckingAlreadyExistsThrowsException_ReturnsInternalServerError()
        {
            //arrange 
            const string version = "0.6";

            string blobName = $"{fundingSchemaFolder}/{version}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();

            fundingSchemaRepository
                .When(x => x.SchemaVersionExists(Arg.Any<string>()))
                .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            FundingSchemaService fundingSchemaService = CreateFundingSchemaService(logger, fundingSchemaRepository: fundingSchemaRepository);

            //Act
            IActionResult result = await fundingSchemaService.GetFundingSchemaByVersion(version);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Error occurred fetching funding schema verion '{version}'");

            logger
               .Received(1)
               .Error(Arg.Is($"Failed to check if funding schema version: '{blobName}' exists"));
        }

        [TestMethod]
        public async Task GetFundingSchemaByVersion_WhenGettingSchemaThrowsException_ReturnsInternalServerError()
        {
            //arrange 
            const string version = "0.6";

            string blobName = $"{fundingSchemaFolder}/{version}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();

            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);

            fundingSchemaRepository
                .When(x => x.GetFundingSchemaVersion(Arg.Is(blobName)))
                .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            FundingSchemaService fundingSchemaService = CreateFundingSchemaService(logger, fundingSchemaRepository: fundingSchemaRepository);

            //Act
            IActionResult result = await fundingSchemaService.GetFundingSchemaByVersion(version);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Error occurred fetching funding schema verion '{version}'");

            logger
               .Received(1)
               .Error(Arg.Any<Exception>(), Arg.Is($"Failed to fetch funding schema '{blobName}' from blob storage"));
        }

        [TestMethod]
        public async Task GetFundingSchemaByVersion_WhenEmptyStringRetrivedFromBlobStorgae_ReturnsInternalServerError()
        {
            //arrange 
            const string version = "0.6";

            string blobName = $"{fundingSchemaFolder}/{version}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();

            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);

            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(string.Empty);

            ILogger logger = CreateLogger();

            FundingSchemaService fundingSchemaService = CreateFundingSchemaService(logger, fundingSchemaRepository: fundingSchemaRepository);

            //Act
            IActionResult result = await fundingSchemaService.GetFundingSchemaByVersion(version);

            //Assert
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to retrive blob contents for funding schema version '{version}'");

            logger
               .Received(1)
               .Error(Arg.Is($"Empty schema returned from blob storage for blob name '{blobName}'"));
        }

        [TestMethod]
        public async Task GetFundingSchemaByVersion_WhenSchemaRetrivedFromBlobStorgae_ReturnsOkResult()
        {
            //arrange 
            const string schema = "schema";

            const string version = "0.6";

            string blobName = $"{fundingSchemaFolder}/{version}.json";

            IFundingSchemaRepository fundingSchemaRepository = CreateFundingSchemaRepository();

            fundingSchemaRepository
                .SchemaVersionExists(Arg.Is(blobName))
                .Returns(true);

            fundingSchemaRepository
                .GetFundingSchemaVersion(Arg.Is(blobName))
                .Returns(schema);

            ILogger logger = CreateLogger();

            FundingSchemaService fundingSchemaService = CreateFundingSchemaService(logger, fundingSchemaRepository: fundingSchemaRepository);

            //Act
            IActionResult result = await fundingSchemaService.GetFundingSchemaByVersion(version);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(schema);
        }

        private static byte[] CreateSchemaFile(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                return stream.ReadAllBytes();
            }
        }

        private static FundingSchemaService CreateFundingSchemaService(
            ILogger logger = null,
            IFundingSchemaRepository fundingSchemaRepository = null)
        {
            return new FundingSchemaService(
                logger ?? CreateLogger(),
                fundingSchemaRepository ?? CreateFundingSchemaRepository(),
                PolicyResiliencePoliciesTestHelper.GenerateTestPolicies());
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IFundingSchemaRepository CreateFundingSchemaRepository()
        {
            return Substitute.For<IFundingSchemaRepository>();
        }
    }
}
