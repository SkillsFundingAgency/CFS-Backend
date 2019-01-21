using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
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
            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

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

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

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

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

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

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

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

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

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
    }
}
