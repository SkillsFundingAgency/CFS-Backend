using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

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
        async public Task SaveDefinition_GivenValidYamlButFailsToGenerateExcelFile_ReturnsInvalidServerError()
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

            byte[] excelAsBytes = new byte[0];

            IExcelWriter<DatasetDefinition> excelWriter = CreateExcelWriter();
            excelWriter
                .Write(Arg.Any<DatasetDefinition>())
                .Returns(excelAsBytes);


            DefinitionsService service = CreateDefinitionsService(logger, datasetsRepository, searchRepository, excelWriter: excelWriter);

            //Act
            IActionResult result = await service.SaveDefinition(request);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to generate excel file for 14/15");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to generate excel file for 14/15"));
        }

        [TestMethod]
        async public Task SaveDefinition_GivenValidYamlButFailsToUploadToBlobStorage_ReturnsInvalidServerError()
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

            byte[] excelAsBytes = new byte[100];

            IExcelWriter<DatasetDefinition> excelWriter = CreateExcelWriter();
            excelWriter
                .Write(Arg.Any<DatasetDefinition>())
                .Returns(excelAsBytes);

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .When(x => x.UploadFromStreamAsync(Arg.Any<Stream>()))
                 .Do(x => { throw new Exception($"Failed to upload 14/15 blob storage"); });

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Is("schemas/14_15.xlsx"))
                .Returns(blob);

            DefinitionsService service = CreateDefinitionsService(logger, datasetsRepository, searchRepository, excelWriter: excelWriter, blobClient: blobClient);

            //Act
            IActionResult result = await service.SaveDefinition(request);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to upload 14/15 blob storage");
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

            byte[] excelAsBytes = new byte[100];

            IExcelWriter<DatasetDefinition> excelWriter = CreateExcelWriter();
            excelWriter
                .Write(Arg.Any<DatasetDefinition>())
                .Returns(excelAsBytes);

            ICloudBlob blob = Substitute.For<ICloudBlob>();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Any<string>())
                .Returns(blob);

            DefinitionsService service = CreateDefinitionsService(logger, datasetsRepository, searchRepository, excelWriter: excelWriter, blobClient: blobClient);

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

            byte[] excelAsBytes = new byte[100];

            IExcelWriter<DatasetDefinition> excelWriter = CreateExcelWriter();
            excelWriter
                .Write(Arg.Any<DatasetDefinition>())
                .Returns(excelAsBytes);

            ICloudBlob blob = Substitute.For<ICloudBlob>();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Any<string>())
                .Returns(blob);

            DefinitionsService service = CreateDefinitionsService(logger, datasetsRepository, searchRepository, excelWriter: excelWriter, blobClient: blobClient);

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
                    i.First().ModelHash == "DFBD0E1ACD29CEBCF5AD45674688D3780D916294C4DF878074AFD01B67BF129C" &&
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
                ModelHash = "DFBD0E1ACD29CEBCF5AD45674688D3780D916294C4DF878074AFD01B67BF129C",
                Name = "14/15",
                ProviderIdentifier = "None",
            };

            ISearchRepository<DatasetDefinitionIndex> searchRepository = CreateDatasetDefinitionSearchRepository();
            searchRepository
                .SearchById(Arg.Is(definitionId))
                .Returns(existingIndex);

            byte[] excelAsBytes = new byte[100];

            IExcelWriter<DatasetDefinition> excelWriter = CreateExcelWriter();
            excelWriter
                .Write(Arg.Any<DatasetDefinition>())
                .Returns(excelAsBytes);

            ICloudBlob blob = Substitute.For<ICloudBlob>();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlockBlobReference(Arg.Any<string>())
                .Returns(blob);

            DefinitionsService service = CreateDefinitionsService(logger, datasetsRepository, searchRepository, excelWriter: excelWriter, blobClient: blobClient);

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

        [TestMethod]
        public async Task GetDatasetSchemaSasUrl_GivenNullRequestModel_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DefinitionsService definitionsService = CreateDefinitionsService(logger);

            //Act
            IActionResult result = await definitionsService.GetDatasetSchemaSasUrl(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("No dataset schema request model was provided");

            logger
                .Received(1)
                .Warning(Arg.Is("No dataset schema request model was provided"));
        }

        [TestMethod]
        public async Task GetDatasetSchemaSasUrl_GivenNullOrEmptyDefinitionName_ReturnsBadRequest()
        {
            //Arrange
            DatasetSchemaSasUrlRequestModel model = new DatasetSchemaSasUrlRequestModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            DefinitionsService definitionsService = CreateDefinitionsService(logger);

            //Act
            IActionResult result = await definitionsService.GetDatasetSchemaSasUrl(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("No dataset schema name was provided");

            logger
                .Received(1)
                .Warning(Arg.Is("No dataset schema name was provided"));
        }

        [TestMethod]
        public async Task GetDatasetSchemaSasUrl_GivenModelAndDatasetNameContainsSlashes_ReplacesSlashesWithUnderscoreAndReturnsUrl()
        {
            //Arrange
            DatasetSchemaSasUrlRequestModel model = new DatasetSchemaSasUrlRequestModel
            {
                DatasetDefinitionName = "14/15"
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IBlobClient blobClient = CreateBlobClient();

            DefinitionsService definitionsService = CreateDefinitionsService(blobClient: blobClient);

            //Act
            IActionResult result = await definitionsService.GetDatasetSchemaSasUrl(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            blobClient
                .Received(1)
                .GetBlobSasUrl(Arg.Is("schemas/14_15.xlsx"), Arg.Any<DateTimeOffset>(), Arg.Any<SharedAccessBlobPermissions>());
        }

        [TestMethod]
        public async Task GetDatasetSchemaSasUrl_GivenModelAndDatasetNameDoesNotContainSlashes_GetSasUrl()
        {
            //Arrange
            const string sasUrl = "https://wherever.naf?jhjhjhjhjhhjhjhjhjjhj";

            DatasetSchemaSasUrlRequestModel model = new DatasetSchemaSasUrlRequestModel
            {
                DatasetDefinitionName = "14 15"
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobSasUrl(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<SharedAccessBlobPermissions>())
                .Returns(sasUrl);

            DefinitionsService definitionsService = CreateDefinitionsService(blobClient: blobClient);

            //Act
            IActionResult result = await definitionsService.GetDatasetSchemaSasUrl(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            DatasetSchemaSasUrlResponseModel responseModel = okObjectResult.Value as DatasetSchemaSasUrlResponseModel;

            responseModel
                .SchemaUrl
                .Should()
                .Be(sasUrl);

            blobClient
                .Received(1)
                .GetBlobSasUrl(Arg.Is("schemas/14 15.xlsx"), Arg.Any<DateTimeOffset>(), Arg.Any<SharedAccessBlobPermissions>());
        }

        static DefinitionsService CreateDefinitionsService(
            ILogger logger = null,
            IDatasetRepository datasetsRepository = null,
            ISearchRepository<DatasetDefinitionIndex> datasetDefinitionSearchRepository = null,
            IDatasetsResiliencePolicies datasetsResiliencePolicies = null,
            IExcelWriter<DatasetDefinition> excelWriter = null,
            IBlobClient blobClient = null)
        {
            return new DefinitionsService(logger ?? CreateLogger(),
                datasetsRepository ?? CreateDataSetsRepository(),
                 datasetDefinitionSearchRepository ?? CreateDatasetDefinitionSearchRepository(),
                 datasetsResiliencePolicies ?? CreateDatasetsResiliencePolicies(),
                 excelWriter ?? CreateExcelWriter(),
                 blobClient ?? CreateBlobClient()
                );
        }

        static IBlobClient CreateBlobClient()
        {
            return Substitute.For<IBlobClient>();
        }

        static IExcelWriter<DatasetDefinition> CreateExcelWriter()
        {
            return Substitute.For<IExcelWriter<DatasetDefinition>>();
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
