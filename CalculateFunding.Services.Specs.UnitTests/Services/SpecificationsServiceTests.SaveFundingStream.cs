using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        async public Task SaveFundingStream_GivenNoYamlWasProvidedWithNoFileName_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty yaml provided for file: File name not provided"));
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenNoYamlWasProvidedButFileNameWas_ReturnsBadRequest()
        {
            //Arrange
            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "yaml-file", new StringValues(yamlFile) }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty yaml provided for file: {yamlFile}"));
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenNoYamlWasProvidedButIsInvalid_ReturnsBadRequest()
        {
            //Arrange
            string yaml = "invalid yaml";
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "yaml-file", new StringValues(yamlFile) }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Invalid yaml was provided for file: {yamlFile}"));
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenValidYamlButFailedToSaveToDatabase_ReturnsStatusCode()
        {
            //Arrange
            string yaml = CreateRawFundingStream();
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "yaml-file", new StringValues(yamlFile) }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            HttpStatusCode failedCode = HttpStatusCode.BadGateway;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .SaveFundingStream(Arg.Any<FundingStream>())
                .Returns(failedCode);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = (StatusCodeResult)result;
            statusCodeResult
                .StatusCode
                .Should()
                .Be(502);

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save yaml file: {yamlFile} to cosmos db with status 502"));
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenValidYamlButSavingToDatabaseThrowsException_ReturnsInternalServerError()
        {
            //Arrange
            string yaml = CreateRawFundingStream();
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "yaml-file", new StringValues(yamlFile) }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .When(x => x.SaveFundingStream(Arg.Any<FundingStream>()))
                .Do(x => { throw new Exception(); });

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = (StatusCodeResult)result;
            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Exception occurred writing to yaml file: {yamlFile} to cosmos db"));
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenValidYamlAndSaveWasSuccesful_ReturnsOK()
        {
            //Arrange
            string yaml = CreateRawFundingStream();
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "yaml-file", new StringValues(yamlFile) }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            HttpStatusCode statusCode = HttpStatusCode.Created;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .SaveFundingStream(Arg.Any<FundingStream>())
                .Returns(statusCode);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Successfully saved file: {yamlFile} to cosmos db"));
        }

        [TestMethod]
        public async Task SaveFundingStream_GivenAllocationLinesWithProviderLookups_ReturnsOK()
        {
            //Arrange
            StringBuilder yaml = new StringBuilder();
            yaml.AppendLine("shortName: GIAS Test");
            yaml.AppendLine("allocationLines:");
            yaml.AppendLine("- fundingRoute: Provider");
            yaml.AppendLine("  isContractRequired: true");
            yaml.AppendLine("  shortName: 16 - 18 Tships Burs Fund");
            yaml.AppendLine("  id: 1618T - 001");
            yaml.AppendLine("  name: 16 - 18 Traineeships Bursary Funding");
            yaml.AppendLine("  providerLookups:");
            yaml.AppendLine("  - providerType: test1");
            yaml.AppendLine("    providerSubType: test2");
            yaml.AppendLine("- fundingRoute: Provider");
            yaml.AppendLine("  isContractRequired: true");
            yaml.AppendLine("  shortName: 16 - 18 Tships Prog Fund");
            yaml.AppendLine("  id: 1618T - 002");
            yaml.AppendLine("  name: 16 - 18 Traineeships Programme Funding");
            yaml.AppendLine("periodType:");
            yaml.AppendLine("  startDay: 1");
            yaml.AppendLine("  startMonth: 8");
            yaml.AppendLine("  endDay: 31");
            yaml.AppendLine("  endMonth: 7");
            yaml.AppendLine("  id: AY");
            yaml.AppendLine("  name: Schools Academic Year");
            yaml.AppendLine("id: GIASTEST");
            yaml.AppendLine("name: GIAS Test");

            byte[] byteArray = Encoding.UTF8.GetBytes(yaml.ToString());
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "yaml-file", new StringValues(yamlFile) }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            HttpStatusCode statusCode = HttpStatusCode.Created;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .SaveFundingStream(Arg.Any<FundingStream>())
                .Returns(statusCode);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<FundingStream[]>(Arg.Is(CacheKeys.AllFundingStreams))
                .Returns(true);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Successfully saved file: {yamlFile} to cosmos db"));

            await specificationsRepository
                .Received(1)
                .SaveFundingStream(Arg.Is<FundingStream>(f => f.AllocationLines.First().ProviderLookups.Count() == 1 && f.AllocationLines.First().ProviderLookups.First().ProviderType == "test1" && f.AllocationLines.First().ProviderLookups.First().ProviderSubType == "test2"));

            await cacheProvider
                .Received(1)
                .KeyDeleteAsync<FundingStream[]>(CacheKeys.AllFundingStreams);
        }
    }
}
