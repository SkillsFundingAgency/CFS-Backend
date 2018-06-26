using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Primitives;
using System.IO;
using CalculateFunding.Models.Datasets.Schema;
using System.Net;
using System.Linq;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DefinitionsServiceTests
    {
        const string yamlFile = "12345.yaml";

        [TestMethod]
        async public Task SaveDefinition_GivenNoYamlWasProvidedWithNoFileName_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DefinitionsService service = CreateDefinitionsService(logger);

            //Act
            IActionResult result = await service.SaveDefinition(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty yaml provided for file: File name not provided"));
        }

        [TestMethod]
        async public Task SaveDefinition_GivenNoYamlWasProvidedButFileNameWas_ReturnsBadRequest()
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

            DefinitionsService service = CreateDefinitionsService(logger);

            //Act
            IActionResult result = await service.SaveDefinition(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty yaml provided for file: {yamlFile}"));
        }

        [TestMethod]
        async public Task SaveDefinition_GivenNoYamlWasProvidedButIsInvalid_ReturnsBadRequest()
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

            DefinitionsService service = CreateDefinitionsService(logger);

            //Act
            IActionResult result = await service.SaveDefinition(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Invalid yaml was provided for file: {yamlFile}"));
        }

        [TestMethod]
        async public Task SaveDefinition_GivenValidYamlButFailedToSaveToDatabase_ReturnsStatusCode()
        {
            //Arrange
            string yaml = CreateRawDefinition();
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

            IDatasetRepository dataSetsRepository = CreateDataSetsRepository();
            dataSetsRepository
                .SaveDefinition(Arg.Any<DatasetDefinition>())
                .Returns(failedCode);

            DefinitionsService service = CreateDefinitionsService(logger, dataSetsRepository);

            //Act
            IActionResult result = await service.SaveDefinition(request);

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
        async public Task SaveDefinition_GivenValidYamlButSavingToDatabaseThrowsException_ReturnsInternalServerError()
        {
            //Arrange
            string yaml = CreateRawDefinition();
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

            IDatasetRepository dataSetsRepository = CreateDataSetsRepository();
            dataSetsRepository
                .When(x => x.SaveDefinition(Arg.Any<DatasetDefinition>()))
                .Do(x => { throw new Exception(); });
                               
            DefinitionsService service = CreateDefinitionsService(logger, dataSetsRepository);

            //Act
            IActionResult result = await service.SaveDefinition(request);

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
        async public Task SaveDefinition_GivenValidYamlAndSaveWasSuccesful_ReturnsOK()
        {
            //Arrange
            string yaml = CreateRawDefinition();
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

            IDatasetRepository dataSetsRepository = CreateDataSetsRepository();
            dataSetsRepository
                .SaveDefinition(Arg.Any<DatasetDefinition>())
                .Returns(statusCode);

            DefinitionsService service = CreateDefinitionsService(logger, dataSetsRepository);

            //Act
            IActionResult result = await service.SaveDefinition(request);

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Successfully saved file: {yamlFile} to cosmos db"));
        }

        [TestMethod]
        async public Task GetDatasetDefinitions_GivenDefinitionsRequestedButContainsNoResults_ReturnsEmptyArray()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            IEnumerable<DatasetDefinition> definitions = new DatasetDefinition[0];

            IDatasetRepository repository = CreateDataSetsRepository();
            repository
                .GetDatasetDefinitions()
                .Returns(definitions);

            DefinitionsService service = CreateDefinitionsService(dataSetsRepository: repository);

            //Act
            IActionResult result = await service.GetDatasetDefinitions(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objResult = (OkObjectResult)result;

            IEnumerable<DatasetDefinition> objValue = (IEnumerable<DatasetDefinition>)objResult.Value;

            objValue
                .Count()
                .Should()
                .Be(0);
        }

        [TestMethod]
        async public Task GetDatasetDefinitions_GivenDefinitionsRequestedButContainsResults_ReturnsArray()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            IEnumerable<DatasetDefinition> definitions = new[]
            {
                new DatasetDefinition(), new DatasetDefinition()
            };

            IDatasetRepository repository = CreateDataSetsRepository();
            repository
                .GetDatasetDefinitions()
                .Returns(definitions);

            DefinitionsService service = CreateDefinitionsService(dataSetsRepository: repository);

            //Act
            IActionResult result = await service.GetDatasetDefinitions(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objResult = (OkObjectResult)result;

            IEnumerable<DatasetDefinition> objValue = (IEnumerable<DatasetDefinition>)objResult.Value;

            objValue
                .Count()
                .Should()
                .Be(2);
        }

        static DefinitionsService CreateDefinitionsService(ILogger logger = null, IDatasetRepository dataSetsRepository = null)
        {
            return new DefinitionsService(logger ?? CreateLogger(), dataSetsRepository ?? CreateDataSetsRepository());
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static IDatasetRepository CreateDataSetsRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }

        static string CreateRawDefinition()
        {
            StringBuilder yaml = new StringBuilder(185);
            yaml.AppendLine(@"id: 9183");
            yaml.AppendLine(@"name: 14/15");
            yaml.AppendLine(@"description: 14/15");
            yaml.AppendLine(@"tableDefinitions:");
            yaml.AppendLine(@"- id: 9189");
            yaml.AppendLine(@"  name: 14/15");
            yaml.AppendLine(@"  description: 14/15");
            yaml.AppendLine(@"  fieldDefinitions:");
            yaml.AppendLine(@"  - id: 9189");
            yaml.AppendLine(@"    name: 14/15");
            yaml.AppendLine(@"    description: 14/15");

            return yaml.ToString();
        }
    }
}
