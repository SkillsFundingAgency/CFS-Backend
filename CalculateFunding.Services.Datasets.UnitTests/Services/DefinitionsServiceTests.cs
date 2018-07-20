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
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Repositories.Common.Search;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DefinitionsServiceTests
    {
        private const string yamlFile = "12345.yaml";

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
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Exception occurred writing to yaml file: 12345.yaml to cosmos db");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Exception occurred writing to yaml file: {yamlFile} to cosmos db"));
        }

        [TestMethod]
        async public Task SaveDefinition_GivenValidYamlAndSearchDoesNotContainExistingItem_ThenSaveWasSuccesfulAndReturnsOK()
        {
            //Arrange
            string yaml = CreateRawDefinition();
            string definitionId = "9183";

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

            IDatasetRepository datasetsRepository = CreateDataSetsRepository();
            datasetsRepository
                .SaveDefinition(Arg.Any<DatasetDefinition>())
                .Returns(statusCode);

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateDatasetDefinitionSearchRepository();
            searchRepository
                .SearchById(Arg.Is(definitionId))
                .Returns((DatasetDefinitionIndex)null);

            DefinitionsService service = CreateDefinitionsService(logger, datasetsRepository, searchRepository);

            //Act
            IActionResult result = await service.SaveDefinition(request);

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();

            await searchRepository
                 .Received(1)
                 .SearchById(Arg.Is(definitionId));

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<DatasetDefinitionIndex>>(
                    i => i.First().Description == "14/15 description" &&
                    i.First().Id == "9183" &&
                    !string.IsNullOrWhiteSpace(i.First().ModelHash) &&
                    i.First().Name == "14/15" &&
                    i.First().ProviderIdentifier == "None"
                   ));

            await datasetsRepository
                .Received(1)
                .SaveDefinition(Arg.Is<DatasetDefinition>(
                    i => i.Description == "14/15 description" &&
                    i.Id == "9183" &&
                    i.Name == "14/15"
                   ));

            logger
                .Received(1)
                .Information(Arg.Is($"Successfully saved file: {yamlFile} to cosmos db"));
        }

        [TestMethod]
        async public Task SaveDefinition_GivenValidYamlAndSearchDoesContainsExistingItemWithModelUpdates_ThenSaveWasSuccesfulAndSearchUpdatedAndReturnsOK()
        {
            //Arrange
            string yaml = CreateRawDefinition();
            string definitionId = "9183";

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

            IDatasetRepository datasetsRepository = CreateDataSetsRepository();
            datasetsRepository
                .SaveDefinition(Arg.Any<DatasetDefinition>())
                .Returns(statusCode);

            DatasetDefinitionIndex existingIndex = new DatasetDefinitionIndex()
            {
                Description = "14/15 description",
                Id = "9183",
                LastUpdatedDate = new DateTimeOffset(2018, 6, 19, 14, 10, 2, TimeSpan.Zero),
                ModelHash = "OLDHASH",
                Name = "14/15",
                ProviderIdentifier = "None",
            };

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateDatasetDefinitionSearchRepository();
            searchRepository
                .SearchById(Arg.Is(definitionId))
                .Returns(existingIndex);

            DefinitionsService service = CreateDefinitionsService(logger, datasetsRepository, searchRepository);

            //Act
            IActionResult result = await service.SaveDefinition(request);

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();

            await searchRepository
                 .Received(1)
                 .SearchById(Arg.Is(definitionId));

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<DatasetDefinitionIndex>>(
                    i => i.First().Description == "14/15 description" &&
                    i.First().Id == "9183" &&
                    i.First().ModelHash == "1A24899BEB5336B654A070ABFCE857EAC1083533751E3F43A5EA0F2F361E3444" &&
                    i.First().Name == "14/15" &&
                    i.First().ProviderIdentifier == "None"
                   ));

            await datasetsRepository
                .Received(1)
                .SaveDefinition(Arg.Is<DatasetDefinition>(
                    i => i.Description == "14/15 description" &&
                    i.Id == "9183" &&
                    i.Name == "14/15"
                   ));

            logger
                .Received(1)
                .Information(Arg.Is($"Successfully saved file: {yamlFile} to cosmos db"));
        }

        [TestMethod]
        async public Task SaveDefinition_GivenValidYamlAndSearchDoesContainsExistingItemWithNoUpdates_ThenDatasetDefinitionSavedInCosmosAndSearchNotUpdatedAndReturnsOK()
        {
            //Arrange
            string yaml = CreateRawDefinition();
            string definitionId = "9183";

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

            IDatasetRepository datasetsRepository = CreateDataSetsRepository();
            datasetsRepository
                .SaveDefinition(Arg.Any<DatasetDefinition>())
                .Returns(statusCode);

            DatasetDefinitionIndex existingIndex = new DatasetDefinitionIndex()
            {
                Description = "14/15 description",
                Id = "9183",
                LastUpdatedDate = new DateTimeOffset(2018, 6, 19, 14, 10, 2, TimeSpan.Zero),
                ModelHash = "1A24899BEB5336B654A070ABFCE857EAC1083533751E3F43A5EA0F2F361E3444",
                Name = "14/15",
                ProviderIdentifier = "None",
            };

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateDatasetDefinitionSearchRepository();
            searchRepository
                .SearchById(Arg.Is(definitionId))
                .Returns(existingIndex);

            DefinitionsService service = CreateDefinitionsService(logger, datasetsRepository, searchRepository);

            //Act
            IActionResult result = await service.SaveDefinition(request);

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();

            await searchRepository
                 .Received(1)
                 .SearchById(Arg.Is(definitionId));

            await searchRepository
                .Received(0)
                .Index(Arg.Any<IEnumerable<DatasetDefinitionIndex>>());

            await datasetsRepository
                .Received(1)
                .SaveDefinition(Arg.Is<DatasetDefinition>(
                    i => i.Description == "14/15 description" &&
                    i.Id == "9183" &&
                    i.Name == "14/15"
                   ));

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

            DefinitionsService service = CreateDefinitionsService(datasetsRepository: repository);

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

            DefinitionsService service = CreateDefinitionsService(datasetsRepository: repository);

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

        static DefinitionsService CreateDefinitionsService(
            ILogger logger = null,
            IDatasetRepository datasetsRepository = null,
            ISearchRepository<DatasetDefinitionIndex> datasetDefinitionSearchRepository = null,
            IDatasetsResiliencePolicies datasetsResiliencePolicies = null)
        {
            return new DefinitionsService(logger ?? CreateLogger(),
                datasetsRepository ?? CreateDataSetsRepository(),
                 datasetDefinitionSearchRepository ?? CreateDatasetDefinitionSearchRepository(),
                 datasetsResiliencePolicies ?? CreateDatasetsResiliencePolicies()
                );
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static IDatasetRepository CreateDataSetsRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }

        private static IDatasetsResiliencePolicies CreateDatasetsResiliencePolicies()
        {
            return DatasetsResilienceTestHelper.GenerateTestPolicies();
        }

        private static ISearchRepository<DatasetDefinitionIndex> CreateDatasetDefinitionSearchRepository()
        {
            return Substitute.For<ISearchRepository<DatasetDefinitionIndex>>();
        }


        static string CreateRawDefinition()
        {
            StringBuilder yaml = new StringBuilder(185);
            yaml.AppendLine(@"id: 9183");
            yaml.AppendLine(@"name: 14/15");
            yaml.AppendLine(@"description: 14/15 description");
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
